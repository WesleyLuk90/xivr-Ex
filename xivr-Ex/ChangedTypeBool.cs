namespace xivr
{


    public unsafe partial class xivr_hooks
    {
        class ChangedTypeBool
        {
            private bool old = false;
            public bool Current
            {
                get => old;
                set
                {
                    Changed = !(value == old);
                    old = value;
                }
            }
            public bool Changed { get; private set; }
            public ChangedTypeBool(bool newVal = false)
            {
                old = newVal;
                Current = newVal;
                Changed = false;
            }
            public ChangedTypeBool Set(bool newVal)
            {
                Current = newVal;
                return this;
            }
        }
    }
}

