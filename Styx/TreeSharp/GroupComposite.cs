using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    public abstract class GroupComposite : Composite
    {
        [DebuggerStepThrough]
        protected GroupComposite(params Composite[] children)
        {
            Children = new List<Composite>(children);
            foreach (var composite in Children)
            {
                if (composite != null)
                    composite.Parent = this;
            }
        }

        public void AddChild(Composite child)
        {
            if (child != null)
            {
                child.Parent = this;
                Children.Add(child);
            }
        }

        public void InsertChild(int index, Composite child)
        {
            if (child != null)
            {
                child.Parent = this;
                Children.Insert(index, child);
            }
        }

        public List<Composite> Children { get; set; }

        public Composite Selection { get; protected set; }

        [DebuggerStepThrough]
        public override void Start(object context)
        {
            CleanupHandlers.Push(new ChildrenCleanupHandler(this, context));
            base.Start(context);
        }

        #region Nested type: ChildrenCleanupHandler

        protected class ChildrenCleanupHandler : CleanupHandler
        {
            [DebuggerStepThrough]
            public ChildrenCleanupHandler(GroupComposite owner, object context)
                : base(owner, context)
            {
            }

            [DebuggerStepThrough]
            protected override void DoCleanup(object context)
            {
                foreach (Composite composite in (Owner as GroupComposite).Children)
                {
                    if (composite != null)
                    {
                        composite.Stop(context);
                    }
                }
            }
        }

        #endregion
    }
}
