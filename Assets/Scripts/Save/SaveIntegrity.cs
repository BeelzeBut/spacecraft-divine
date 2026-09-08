using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Signs save payloads so a hand-edited gem balance is detectable. This raises the cost
    /// of casual tampering; it is not a defence against a determined attacker with the
    /// binary, which would require server-side authority we deliberately do not have.
    /// </summary>
    public static class SaveIntegrity
    {
        // Split so the literal string does not appear contiguously in the binary.
        private const string SecretA = "sd-4f21a9c7";
        private const string SecretB = "e30b-koda";

        private static byte[] Key()
        {
            string deviceSalt = SystemInfo.deviceUniqueIdentifier ?? "nodevice";
            return Encoding.UTF8.GetBytes(SecretA + deviceSalt + SecretB);
        }

        public static string Sign(string payload)
        {
            if (payload == null) payload = string.Empty;
            using (var hmac = new HMACSHA256(Key()))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return Convert.ToBase64String(hash);
            }
        }

        public static bool Verify(string payload, string signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;

            string expected = Sign(payload);
            if (expected.Length != signature.Length) return false;

            // Constant-time compare, so failure timing does not leak the expected value.
            int diff = 0;
            for (int i = 0; i < expected.Length; i++)
                diff |= expected[i] ^ signature[i];
            return diff == 0;
        }
    }
}
