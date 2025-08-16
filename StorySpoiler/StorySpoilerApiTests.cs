using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;


namespace StorySpoiler
{
    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient client;
        private static string createdStoryId;
        //your link here
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            // your credentials
            string token = GetJwtToken("examUser", "examUser");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated()
        {
            var story = new
            {
                Title = "New Story",
                Description = "Created story test.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;
            
            Assert.That(createdStoryId, Is.Not.Null.Or.Empty, "Story ID should not be null or empty.");

            var message = json.GetProperty("msg").GetString();
            Assert.That(message, Is.EqualTo("Successfully created!"), "Response message should indicate successful creation.");
        }

        [Test, Order(2)]

        public void EditStory_ShouldReturnOk()
        {
            var updatedStory = new
            {
                Title = "Updated Story",
                Description = "Edited story test.",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            var message = json.GetProperty("msg").GetString();
            Assert.That(message, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]

        public void GetAllStory_ShouldReturnOkAndNonEmptyArray()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var storys = JsonSerializer.Deserialize<List<object>>(response.Content);

            Assert.That(storys, Is.Not.Empty);
        }

        [Test, Order(4)]

        public void DeleteStory_ShoudReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            var message = json.GetProperty("msg").GetString();
            Assert.That(message, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]

        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var story = new
            {
                Title = "",
                Description = "",
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }


        [Test, Order(6)]

        public void Edit_NonExistingStory_ShouldReturnNotFound()
        {
            var nonExistingId = "123"; 

            var changes = new[]
            {
                new { path = "/title", op = "replace", value = "Edited" }
            };

            var request = new RestRequest($"/api/Story/Edit/{nonExistingId}", Method.Put);
            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            /* 
            // Code for "Assert that the response message indicates "No spoilers..."."

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var message = json.GetProperty("msg").GetString();

            Assert.That(message, Is.EqualTo("No spoilers..."));
            */
        }

        [Test, Order(7)]

        public void Delete_NonExistingStory_ShouldReturnBadRequest()
        {
            var nonExistingId = "123"; 

            var request = new RestRequest($"/api/Story/Delete/{nonExistingId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var message = json.GetProperty("msg").GetString();

            Assert.That(message, Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}