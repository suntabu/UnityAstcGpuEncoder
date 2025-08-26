using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace ASTCEncoder
{
    public interface INativeArray<T> where T : unmanaged
    {
        public int Length { get; }
        public T this[int index] { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Array4<T> : INativeArray<T> where T : unmanaged
    {
        public T a0;
        public T a1;
        public T a2;
        public T a3;

        public int Length => 4;
        
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
    public struct Array16<T> : INativeArray<T>, IEnumerable where T : unmanaged
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

        public int Length => 16;
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


    [StructLayout(LayoutKind.Sequential)]
    public struct Array25<T>: INativeArray<T>, IEnumerable<T> where T : unmanaged
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
        public T a16;
        public T a17;
        public T a18;
        public T a19;
        public T a20;
        public T a21;
        public T a22;
        public T a23;
        public T a24;
        
        public int Length => 25;

        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return a0;
                    case 1: return a1;
                    case 2: return a2;
                    case 3: return a3;
                    case 4: return a4;
                    case 5: return a5;
                    case 6: return a6;
                    case 7: return a7;
                    case 8: return a8;
                    case 9: return a9;
                    case 10: return a10;
                    case 11: return a11;
                    case 12: return a12;
                    case 13: return a13;
                    case 14: return a14;
                    case 15: return a15;
                    case 16: return a16;
                    case 17: return a17;
                    case 18: return a18;
                    case 19: return a19;
                    case 20: return a20;
                    case 21: return a21;
                    case 22: return a22;
                    case 23: return a23;
                    case 24: return a24;
                    default:
                        throw new IndexOutOfRangeException(nameof(index));
                }
            }
            set
            {
                switch (index)
                {
                    case 0: a0 = value; break;
                    case 1: a1 = value; break;
                    case 2: a2 = value; break;
                    case 3: a3 = value; break;
                    case 4: a4 = value; break;
                    case 5: a5 = value; break;
                    case 6: a6 = value; break;
                    case 7: a7 = value; break;
                    case 8: a8 = value; break;
                    case 9: a9 = value; break;
                    case 10: a10 = value; break;
                    case 11: a11 = value; break;
                    case 12: a12 = value; break;
                    case 13: a13 = value; break;
                    case 14: a14 = value; break;
                    case 15: a15 = value; break;
                    case 16: a16 = value; break;
                    case 17: a17 = value; break;
                    case 18: a18 = value; break;
                    case 19: a19 = value; break;
                    case 20: a20 = value; break;
                    case 21: a21 = value; break;
                    case 22: a22 = value; break;
                    case 23: a23 = value; break;
                    case 24: a24 = value; break;
                    default:
                        throw new IndexOutOfRangeException(nameof(index));
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < 25; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int _addIndex;

        public void Add(T v)
        {
            this[_addIndex % 25] = v;
            _addIndex++;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Array36<T> : INativeArray<T>, IEnumerable<T> where T : unmanaged
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
        public T a16;
        public T a17;
        public T a18;
        public T a19;
        public T a20;
        public T a21;
        public T a22;
        public T a23;
        public T a24;
        public T a25;
        public T a26;
        public T a27;
        public T a28;
        public T a29;
        public T a30;
        public T a31;
        public T a32;
        public T a33;
        public T a34;
        public T a35;

        public int Length => 36;
        
        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return a0;
                    case 1: return a1;
                    case 2: return a2;
                    case 3: return a3;
                    case 4: return a4;
                    case 5: return a5;
                    case 6: return a6;
                    case 7: return a7;
                    case 8: return a8;
                    case 9: return a9;
                    case 10: return a10;
                    case 11: return a11;
                    case 12: return a12;
                    case 13: return a13;
                    case 14: return a14;
                    case 15: return a15;
                    case 16: return a16;
                    case 17: return a17;
                    case 18: return a18;
                    case 19: return a19;
                    case 20: return a20;
                    case 21: return a21;
                    case 22: return a22;
                    case 23: return a23;
                    case 24: return a24;
                    case 25: return a25;
                    case 26: return a26;
                    case 27: return a27;
                    case 28: return a28;
                    case 29: return a29;
                    case 30: return a30;
                    case 31: return a31;
                    case 32: return a32;
                    case 33: return a33;
                    case 34: return a34;
                    case 35: return a35;
                    default:
                        throw new IndexOutOfRangeException(nameof(index));
                }
            }
            set
            {
                switch (index)
                {
                    case 0: a0 = value; break;
                    case 1: a1 = value; break;
                    case 2: a2 = value; break;
                    case 3: a3 = value; break;
                    case 4: a4 = value; break;
                    case 5: a5 = value; break;
                    case 6: a6 = value; break;
                    case 7: a7 = value; break;
                    case 8: a8 = value; break;
                    case 9: a9 = value; break;
                    case 10: a10 = value; break;
                    case 11: a11 = value; break;
                    case 12: a12 = value; break;
                    case 13: a13 = value; break;
                    case 14: a14 = value; break;
                    case 15: a15 = value; break;
                    case 16: a16 = value; break;
                    case 17: a17 = value; break;
                    case 18: a18 = value; break;
                    case 19: a19 = value; break;
                    case 20: a20 = value; break;
                    case 21: a21 = value; break;
                    case 22: a22 = value; break;
                    case 23: a23 = value; break;
                    case 24: a24 = value; break;
                    case 25: a25 = value; break;
                    case 26: a26 = value; break;
                    case 27: a27 = value; break;
                    case 28: a28 = value; break;
                    case 29: a29 = value; break;
                    case 30: a30 = value; break;
                    case 31: a31 = value; break;
                    case 32: a32 = value; break;
                    case 33: a33 = value; break;
                    case 34: a34 = value; break;
                    case 35: a35 = value; break;
                    default:
                        throw new IndexOutOfRangeException(nameof(index));
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < 36; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int _addIndex;

        public void Add(T v)
        {
            this[_addIndex % 36] = v;
            _addIndex++;
        }
    }
}