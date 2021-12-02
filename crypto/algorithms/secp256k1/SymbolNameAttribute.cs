using System;

namespace wallet_beautifier.crypto.algorithms.secp256k1
{
    class SymbolNameAttribute : Attribute
    {
        public readonly string Name;

        public SymbolNameAttribute(string name)
        {
            Name = name;
        }
    }


}