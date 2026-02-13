using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// 1. Setup Database Connection
string connectionString = "Host=xxxxx;Port=xxxxx;Database=your_db;Username=your_username;Password=your_password";//Postgres Connection String
var dbService = new DatabaseService(connectionString);

// 2. Configure the Cloud-Hosted Open Source Model
var builder = Kernel.CreateBuilder();

// Replace 'YOUR_API_KEY' with the key from your provider Eg : Ollama
// Replace 'YOUR_ENDPOINT_URL' with the provider's base URL (e.g., Together AI, OpenRouter)
builder.AddOpenAIChatCompletion(
    modelId: "gpt-oss:120b-cloud",
    apiKey: "YOUR_API_KEY",
    endpoint: new Uri("https://ollama.com/v1")
);

var kernel = builder.Build();

// 3. Extract DB Schema to provide context to the 120B model
string schema = dbService.GetSchemaContext();

while (true)
{
    Console.Write("\nAsk your DB a question (or type 'exit'): ");
    string? userQuestion = Console.ReadLine();
    if (string.IsNullOrEmpty(userQuestion) || userQuestion.ToLower() == "exit") break;

    // 4. Construct the Prompt for the 120B Model
    // Larger models handle complex reasoning better, so we give it strict instructions.
    string systemPrompt = $@"
        You are a highly advanced SQL Assistant. 
        Your task is to convert natural language into valid PostgreSQL queries.
        
        SCHEMA CONTEXT:
        {schema}
        
        RULES:
        1. Return ONLY the SQL code. 
        2. Do not use Markdown code blocks (```sql).
        3. Use only the tables and columns provided in the schema.
        
        USER QUESTION: {userQuestion}";

    var chatService = kernel.GetRequiredService<IChatCompletionService>();

    try
    {
        var response = await chatService.GetChatMessageContentAsync(systemPrompt);
        string generatedSql = response.ToString().Trim();

        // Basic cleaning in case the model ignores 'no markdown' instructions
        generatedSql = generatedSql.Replace("```sql", "").Replace("```", "").Trim();

        Console.WriteLine($"\n[Generated SQL]: {generatedSql}");

        // 5. Execute
        string result = dbService.ExecuteQuery(generatedSql);
        Console.WriteLine($"\n[Results]:\n{result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"AI Error: {ex.Message}");
    }

}
