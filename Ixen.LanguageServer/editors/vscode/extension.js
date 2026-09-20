const path = require("path");
const vscode = require("vscode");
const { LanguageClient, TransportKind } = require("vscode-languageclient/node");

let client;

function serverPath(context) {
    const configured = vscode.workspace.getConfiguration("ixen").get("server.path");

    return configured ? configured : context.asAbsolutePath(path.join("server", "Ixen.LanguageServer.dll"));
}

function activate(context) {
    const dotnet = vscode.workspace.getConfiguration("ixen").get("server.dotnet") || "dotnet";
    const server = { command: dotnet, args: ["exec", serverPath(context)], transport: TransportKind.stdio };

    client = new LanguageClient("ixen", "Ixen", { run: server, debug: server }, {
        documentSelector: [{ language: "xnl" }, { language: "xns" }],
        synchronize: { fileEvents: vscode.workspace.createFileSystemWatcher("**/*.{xnl,xns}") }
    });

    return client.start();
}

function deactivate() {
    return client ? client.stop() : undefined;
}

module.exports = { activate, deactivate };
