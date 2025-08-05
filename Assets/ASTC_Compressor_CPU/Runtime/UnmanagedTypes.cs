using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace LIBII
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Array4<T> where T : unmanaged
    {
        public T a0;
        public T a1;
        public T a2;
        public T a3;

        public T this[uint index]
        {
            get => this[(int)index];
            set => this[(int)index] = value;
        }

        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return a0;
                    case 1:
                        return a1;
                    case 2:
                        return a2;
                    case 3:
                        return a3;
                    default:
                        throw new System.IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        a0 = value;
                        break;
                    case 1:
                        a1 = value;
                        break;
                    case 2:
                        a2 = value;
                        break;
                    case 3:
                        a3 = value;
                        break;
                }
            }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Array8<T> where T : unmanaged
    {
        public T a0;
        public T a1;
        public T a2;
        public T a3;
        public T a4;
        public T a5;
        public T a6;
        public T a7;

        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return a0;
                    case 1:
                        return a1;
                    case 2:
                        return a2;
                    case 3:
                        return a3;
                    case 4:
                        return a4;
                    case 5:
                        return a5;
                    case 6:
                        return a6;
                    case 7:
                        return a7;
                    default:
                        throw new System.IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        a0 = value;
                        break;
                    case 1:
                        a1 = value;
                        break;
                    case 2:
                        a2 = value;
                        break;
                    case 3:
                        a3 = value;
                        break;
                    case 4:
                        a4 = value;
                        break;
                    case 5:
                        a5 = value;
                        break;
                    case 6:
                        a6 = value;
                        break;
                    case 7:
                        a7 = value;
                        break;
                }
            }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Array16<T> : IEnumerable where T : unmanaged
    {
        public T a0;
        public T a1;
        public T a2;
        public T a3;
        public T a4;
        public T a5;
        public T a6;
        public T a7;
        public T a8;
        public T a9;
        public T a10;
        public T a11;
        public T a12;
        public T a13;
        public T a14;
        public T a15;

        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return a0;
                    case 1:
                        return a1;
                    case 2:
                        return a2;
                    case 3:
                        return a3;
                    case 4:
                        return a4;
                    case 5:
                        return a5;
                    case 6:
                        return a6;
                    case 7:
                        return a7;
                    case 8:
                        return a8;
                    case 9:
                        return a9;
                    case 10:
                        return a10;
                    case 11:
                        return a11;
                    case 12:
                        return a12;
                    case 13:
                        return a13;
                    case 14:
                        return a14;
                    case 15:
                        return a15;
                    default:
                        throw new System.IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        a0 = value;
                        break;
                    case 1:
                        a1 = value;
                        break;
                    case 2:
                        a2 = value;
                        break;
                    case 3:
                        a3 = value;
                        break;
                    case 4:
                        a4 = value;
                        break;
                    case 5:
                        a5 = value;
                        break;
                    case 6:
                        a6 = value;
                        break;
                    case 7:
                        a7 = value;
                        break;
                    case 8:
                        a8 = value;
                        break;
                    case 9:
                        a9 = value;
                        break;
                    case 10:
                        a10 = value;
                        break;
                    case 11:
                        a11 = value;
                        break;
                    case 12:
                        a12 = value;
                        break;
                    case 13:
                        a13 = value;
                        break;
                    case 14:
                        a14 = value;
                        break;
                    case 15:
                        a15 = value;
                        break;
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            var list = new List<T>() { a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15 };
            return list.GetEnumerator();
        }

        private int _addIndex;

        public void Add(T v)
        {
            this[_addIndex % 16] = v;

            _addIndex++;
        }
    }
}