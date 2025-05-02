using System.IO;
using System.Threading.Tasks;

namespace LuciReality.AI
{
  public static class TextQuerySchema
  {
    public async static Task<string> ResponseSchema() {
      using var reader = new StreamReader("text_response_schema.json");
      return await reader.ReadToEndAsync();
    }

    public async static Task<string> Instructions()
    {
      using var reader = new StreamReader("text_query_instructions.json");
      return await reader.ReadToEndAsync();
    }
  }
}