using System.Threading.Tasks;

namespace wallet_beautifier.crypto.coin
{
    public interface ICoin
    {
        ///<summary>
        /// Returns common name for this coin.
        ///</summary>
        string GetCommonName { get; } 

        ///<summary>
        /// Returns ticker symbol for this coin.
        ///</summary>
        string GetTicker { get; } 

        ///<summary>
        /// Returns the subdirectory where it will store it's attempts.
        ///</summary>
        string GetAttemptPath { get; } 

        ///<summary>
        /// Returns the curve used to generate a public key (from the random bytes of private key).
        ///</summary>
        CurveType GetCurveType { get; }

        ///<summary>
        /// Returns the post calculation used to calculate a string from a public key byte array (may not be final for for a crypto).
        ///</summary>
        PostCalculationType GetPostCalculationType { get; }

        ///<summary>
        /// Generate a wallet address from public key string, could have some calculation, depending on implementation (e.g: hash).
        ///</summary>
        string GenerateAddressFromCalculatedPublicKey(string calculatedPublicKey);


        ///<summary>
        /// Tweaks private key as needed.
        ///</summary>
        byte[] TweakPrivateKey(byte[] privateKey);

        Task BufferKeyPairAsync(string address, byte[] privateKey);

        bool CharactersAreAllowedInPublicAddress(string address, bool termsCaseSensitive);

        void Open();

        Task CloseAsync();
    }
}