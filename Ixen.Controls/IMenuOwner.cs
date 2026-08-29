namespace Ixen.Controls
{
    internal interface IMenuOwner
    {
        bool IsVertical { get; }

        bool HoverOpens { get; }

        void CloseSubmenus(MenuItem except);

        void ItemActivated(MenuItem item);

        void Changed();
    }
}
