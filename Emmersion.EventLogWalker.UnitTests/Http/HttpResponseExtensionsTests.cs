using Emmersion.EventLogWalker.Http;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests.Http
{
    public class WhenDeserializingAJsonResponse
    {
        private HttpResponse response;
        private JsonTest deserialized;
        
        [SetUp]
        public void SetUp()
        {
            var json = "{\"stringProperty\":\"hello world\",\"IntegerProperty\":123}";
            response = new HttpResponse(200, new HttpHeaders(), json);
            deserialized = new JsonSerializer().Deserialize<JsonTest>(response.Body);
        }

        [Test]
        public void ShouldReturnAnObjectWithTheCorrectProperties()
        {
            Assert.That(deserialized.StringProperty, Is.EqualTo("hello world"));
            Assert.That(deserialized.IntegerProperty, Is.EqualTo(123));
        }
    }
}