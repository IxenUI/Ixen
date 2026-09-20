# Editors

`Ixen.LanguageServer` speaks the Language Server Protocol over stdin and stdout, so any editor that
can start a process and talk LSP to it gets XNL and XNS support: live diagnostics, completion,
go-to-definition and colouring from Ixen's own tokenizer rather than from a regular expression.

Build it once:

```
dotnet publish Framework/Ixen.LanguageServer -c Release
```

and start it with `dotnet exec <path>/Ixen.LanguageServer.dll`. It reads no arguments and writes
nothing to stdout but LSP messages.

## Visual Studio Code

`vscode/` is the client. It is about thirty lines, because everything it does is start the server
and hand it the two file extensions.

```
cd Framework/Ixen.LanguageServer/editors/vscode
npm install
```

Then either copy the published server into `vscode/server/`, or point `ixen.server.path` at it in
your settings. `code --extensionDevelopmentPath=<this folder>` opens a window with it loaded.

## Anything else

There is no client for the others because none of them needs one written here — the server is what
they all consume.

**Neovim**, with the built-in client:

```lua
vim.filetype.add({ extension = { xnl = "xnl", xns = "xns" } })

vim.lsp.start({
    name = "ixen",
    cmd = { "dotnet", "exec", "/path/to/Ixen.LanguageServer.dll" },
    root_dir = vim.fs.dirname(vim.fs.find({ ".git" }, { upward = true })[1]),
})
```

**Rider** and the rest of the JetBrains family go through the LSP API of their own plugin system;
**Zed**, **Helix** and **Emacs** (`eglot`) each take the same command line in their own
configuration file.

## What the server provides

| | |
|---|---|
| `textDocument/publishDiagnostics` | `XN001` to `XN009`, on every open and every change |
| `textDocument/semanticTokens/full` | eight token types, from the XNL and XNS tokenizers |
| `textDocument/completion` | the same proposals the Visual Studio extension makes |
| `textDocument/definition` | a variable, a mixin, a keyframes set, and a view's names to the rules that style them |

Synchronisation is **full text** rather than incremental: a whole re-tokenise of the largest
stylesheet in the Ixen workspace costs about a third of a millisecond, so there is nothing an
incremental path would buy.
