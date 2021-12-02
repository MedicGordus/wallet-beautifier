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
        /// Returns the stage at which GenerateAddressFromPublicKey wants to grab it's bytes at.
        ///</summary>
        ByteStage GetByteStage { get; } 

        ///<summary>
        /// Generate a wallet address form public key bytes.
        ///</summary>
        string GenerateAddressFromHashedPublicKey(byte[] publicKey);

        Task BufferKeyPairAsync(string address, byte[] privateKey);

        bool CharactersAreAllowedInPublicAddress(string address);

        void Open();

        Task CloseAsync();
    }
}