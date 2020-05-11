using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework;
using TailSpin.SpaceGame.Web;
using TailSpin.SpaceGame.Web.Models;

// using Microsoft.Azure.KeyVault;
// using Microsoft.IdentityModel.Clients.ActiveDirectory;
// using Microsoft.Azure.Management.ResourceManager.Fluent;
// using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Tests
{
    public class DocumentDBRepository_GetItemsAsyncShould
    {
        private IDocumentDBRepository<Score> _scoreRepository;

        
        [SetUp]
        public void Setup()
        {
            using (Stream scoresData = typeof(IDocumentDBRepository<Score>)
                .Assembly
                .GetManifestResourceStream("Tailspin.SpaceGame.Web.SampleData.scores.json"))
            {
                _scoreRepository = new LocalDocumentDBRepository<Score>(scoresData);
            }
        }

        [TestCase("Milky Way")]
        [TestCase("Andromeda")]
        [TestCase("Pinwheel")]
        [TestCase("NGC 1300")]
        [TestCase("Messier 82")]
        public void FetchOnlyRequestedGameRegion(string gameRegion)
        {
            const int PAGE = 0; // take the first page of results
            const int MAX_RESULTS = 10; // sample up to 10 results

            // Form the query predicate.
            // This expression selects all scores for the provided game region.
            Expression<Func<Score, bool>> queryPredicate = score => (score.GameRegion == gameRegion);

            // Fetch the scores.
            Task<IEnumerable<Score>> scoresTask = _scoreRepository.GetItemsAsync(
                queryPredicate, // the predicate defined above
                score => 1, // we don't care about the order
                PAGE,
                MAX_RESULTS
            );
            IEnumerable<Score> scores = scoresTask.Result;

            // Verify that each score's game region matches the provided game region.
            Assert.That(scores, Is.All.Matches<Score>(score => score.GameRegion == gameRegion));
        }

        [TestCase("KeyVaultTest")]
        public void KeyVaultTest(string gameRegion)
        {
                        // kvURL must be updated to the URL of your key vault
            string kvURL = "https://testvaultpoc.vault.azure.net/";

            // <authentication>

            string clientId = "56a49dc5-65f2-476c-8158-2f4867eea9ce";
            string clientSecret = "u80Js/93GDWbjjQU_wwQ.zyzKuLRrG-Q";

            // KeyVaultClient kvClient = new KeyVaultClient(async (authority, resource, scope) =>
            // {
            //     var adCredential = new ClientCredential(clientId, clientSecret);
            //     var authenticationContext = new AuthenticationContext(authority, null);
            //     return (await authenticationContext.AcquireTokenAsync(resource, adCredential)).AccessToken;
            // });
            // // </authentication>

            // var fetchedSecret = GetSecret(kvClient, kvURL, "vehicle");

            // string secretValue = fetchedSecret.Result;
            // test again 1
            SecretClientOptions options = new SecretClientOptions()
                {
                    Retry =
                    {
                        Delay= TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(16),
                    }
                };
            var client = new SecretClient(new Uri(kvURL), new DefaultAzureCredential(),options);

            KeyVaultSecret secret = client.GetSecret("vehicle");

            string secretValue = secret.Value;

            // Verify that each score's game region matches the provided game region.
            Assert.AreEqual(secretValue,"maruti");
        }

        // public async Task<string> GetSecret(KeyVaultClient kvClient, string kvURL, string secretName)
        // {
        //     // <getsecret>                
        //     var keyvaultSecret = await kvClient.GetSecretAsync($"{kvURL}", secretName).ConfigureAwait(false);
        //     // </getsecret>
        //     return keyvaultSecret.Value;
        // }
    }
}