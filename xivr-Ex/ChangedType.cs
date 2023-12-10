using System.Collections.Generic;

namespace xivr
{

    class ChangedType<T>
    {
        private T old = default(T);
        public T Current
        {
            get => old;
            set
            {
                Changed = false;
                if (!EqualityComparer<T>.Default.Equals(value, old))
                {
                    old = value;
                    Changed = true;
                }
            }
        }
        public bool Changed { get; private set; }
        public ChangedType(T newVal = default(T))
        {
            old = newVal;
            Current = newVal;
            Changed = false;
        }
        public ChangedType<T> Set(T newVal)
        {
            Current = newVal;
            return this;
        }
    }
}

