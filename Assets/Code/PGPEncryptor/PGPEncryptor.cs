using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using PgpCore;

namespace EPH.Functions
{
    public static class PGPEncryptor
    {
        [FunctionName("PGPEncryptor")]
        public static async Task Run([ServiceBusTrigger("contracts", Connection = "azureintegrationplatformserverlessapplications_SERVICEBUS")] dynamic myQueueItem,
            [Blob("contracts/{sys.utcnow}.txt", FileAccess.Write)] Stream contract,
            ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message");
            string content = myQueueItem.content;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var encrypted = await EncryptAsync(stream, pgpKey);
            encrypted.CopyTo(contract);
        }

        private static async Task<Stream> EncryptAsync(Stream inputStream, string publicKey)
        {
            using (PGP pgp = new PGP())
            {
                Stream outputStream = new MemoryStream();

                using (inputStream)
                {
                    using (Stream publicKeyStream = GenerateStreamFromString(publicKey))
                    {
                        await pgp.EncryptStreamAsync(inputStream, outputStream, publicKeyStream);
                        outputStream.Seek(0, SeekOrigin.Begin);
                        return outputStream;
                    }
                }
            }
        }

        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private static string pgpKey = @"-----BEGIN PGP PUBLIC KEY BLOCK-----
Version: OpenPGP.js v.1.20130420
Comment: http://openpgpjs.org

xk0EW0Dn2QEB/1CZ0zUTV5LfX1/aDYv1OyryyP9qsl/9Ei5JnoV4bsDfFo/H
ga50pRqzaPC1EPxNoKWAPNLAsc9Yjs6mVO8quLMAEQEAAc0kVGVzdCBNY1Rl
c3Rpbmd0b24gPHRlc3RAZXhhbXBsZS5jb20+wlwEEAEIABAFAltA59oJEOfI
3LZ/mamzAAD8pwH5AYgOmEqcsUDXOUQQTb5pX3Un3sYAQP0+XL7u5rW17f/N
pjztYHjUR58qYVddUBVvFXJpONWCxZPyw76xekMrdA==
=H8zU
-----END PGP PUBLIC KEY BLOCK-----";
    }
}
