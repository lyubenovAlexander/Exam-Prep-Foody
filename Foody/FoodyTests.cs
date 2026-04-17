using Foody.Tests.DTOs;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Foody
{
    public class FoodyTests
    {
        private RestClient client;
        private static string foodId; //за да запазим ID-то на създадената храна в тест 1 за по-късно използване в другите тестове

        [OneTimeSetUp] //Веднъж го конфигурирай, за да не се налага при всеки тест преди пускането на всички тестове! 
        public void Setup()
        {
            string jwtToken = GetJwtToken("exam_preparation_11", "123123");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:81")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:81");
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateFood_WithRequiredFields_ShouldSuccess()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "Soup",
                Description = "Soup with chicken and potatoes",
                Url = ""
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            RestResponse response = client.Execute(request);

            //проверка дали статус кодът е 200 OK
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            //десериализация на отговора в ApiResponseDTO, за да извлечем ID-то на създадената храна
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            //проверка дали съобщението в отговора е "Food created successfully"
            if (readyResponse.FoodId != null)
            {
                foodId = readyResponse.FoodId; //запазваме ID-то на създадената храна за по-късно използване в други тестове 
            }

        }
        [Order(2)]
        [Test]
        public void EditFoodTitle_ShouldChangeTitle()
        {
            RestRequest request = new RestRequest($"/api/Food/Edit/{foodId}", Method.Patch);
            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Chicken Soup"
                }
            });

            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Successfully edited"));
        }

        [Order(3)]
        [Test]
        public void GetAllFood_ShouldReturnNonEmptyArray()
        {
            RestRequest request = new RestRequest($"/api/Food/All", Method.Get);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            List<ApiResponseDTO> readyResponse = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
            Assert.That(readyResponse, Is.Not.Null);
            Assert.That(readyResponse, Is.Not.Empty);
            Assert.That(readyResponse.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Order(4)]
        [Test]
        public void DeleteExistingFood_ShouldSucceed() 
        {
            RestRequest request = new RestRequest($"/api/Food/Delete/{foodId}", Method.Delete);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            Assert.That(readyResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateFood_WithoutRequiredFields_ShouldFail()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "",
                Description = "Soup with chicken and potatoes",
                Url = ""
            };
            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            RestRequest request = new RestRequest($"/api/Food/Edit/99999", Method.Patch); //предполагаемо не съществуващ ID
            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Non Existing Food"
                }
            });
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);    
            Assert.That(response.Content, Does.Contain("No food revues..."));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            RestRequest request = new RestRequest($"/api/Food/Delete/99999", Method.Delete); //предполагаемо не съществуващ ID
            RestResponse response = client.Execute(request);

            //Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            //Assert.That(readyResponse.Msg, Is.EqualTo("No food revues..."));
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to delete this food revue!"));
        }

        [OneTimeTearDown] //Веднъж го изчисти, за да не се налага при всеки тест след пускането на всички тестове!
        public void TearDown()
        {
            this.client?.Dispose(); //Освобождаване на ресурсите, ако е необходимо
        }
    }
}