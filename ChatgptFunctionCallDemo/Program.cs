using ChatGptFunctionCallProcessor;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

var key = "sk-Ab...jW";
var openAiService = new OpenAIService(new OpenAiOptions()
{
    ApiKey = key
});
var email = "testmyemail@goolg.com";
var userprompt = $"我想分别获取成都市今天和西安市明天的天气情况，并发送到{email}这个邮箱";
Console.WriteLine($"user:{userprompt}");
var center = new FunctionCallCentner();
var messages = new List<ChatMessage>
    {
        ChatMessage.FromSystem("You are a helpful assistant."),
        ChatMessage.FromUser(userprompt)
    };
await SessionExecute(messages);
async Task SessionExecute(List<ChatMessage> messages)
{
    var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
    {
        Messages = messages,
        Model = Models.Gpt_3_5_Turbo_0613,
        Functions = center.GetDefinition().ToList()
    });
    if (completionResult.Successful)
    {
        if (completionResult.Choices.First().Message.FunctionCall != null)
        {
            completionResult.Choices.First().Message.Content = "";
            messages.Add(completionResult.Choices.First().Message);
            messages.Add(await center.CallFunction(completionResult.Choices.First().Message.FunctionCall.Name, completionResult.Choices.First().Message.FunctionCall.ParseArguments()));
            await SessionExecute(messages);
        }
        else
        {
            Console.WriteLine("assistant:" + completionResult.Choices.First().Message.Content);
        }
    }
}


public class FunctionCallCentner
{
    [Description("查询用户希望的日期对应的真实日期")]
    public async Task<CommonOutput> GetDate(GetDayInput input)
    {
        await Task.CompletedTask;
        Console.WriteLine($"system:GetDate函数调用触发，参数：city={input.DateType}");
        return new CommonOutput() { data = new GetDayOutput { Date = DateTime.Now.AddDays(input.DateType == DateType.Yesterday ? -1 : input.DateType == DateType.Tomorrow ? 1 : input.DateType == DateType.DayAfterTomorrow ? 2 : 0).ToShortDateString(), }, Success = true };
    }
    [Description("根据城市和真实日期获取天气信息")]
    public async Task<CommonOutput> GetWeather(GetWeatherInput input)
    {
        if (!DateTime.TryParse(input.Date, out _))
            return new CommonOutput() { Success = false, message = "日期格式错误" };
        await Task.CompletedTask;
        Console.WriteLine($"system:GetWeather函数调用触发，参数：city={input.City}，date={input.Date}");
        return new CommonOutput() { data = new GetWeatherOutput { City = input.City, Date = input.Date, Weather = "overcast to cloudy", TemperatureRange = "22˚C-28˚C" }, Success = true };
    }
    [Description("向目标邮箱发送电子邮件")]
    public async Task<CommonOutput> SendEmail(SendEmailInput input)
    {
        await Task.CompletedTask;
        Console.WriteLine($"system:SendEmail函数调用触发，参数：targetemail={input.TargetEmail}，content={input.Content}");
        return new CommonOutput() { Success = true };
    }
}
public class GetDayInput
{
    [Description("日期枚举")]
    public DateType DateType { get; set; }
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DateType
{
    Yesterday,
    Today,
    Tomorrow,
    DayAfterTomorrow
}
public class GetDayOutput
{
    public string Date { get; set; }
}
public class GetWeatherInput
{
    [Description("城市名称")]
    public string City { get; set; }
    [Description("真实日期,格式：yyyy/mm/dd")]
    public string Date { get; set; }
}
public class GetWeatherOutput: GetWeatherInput
{
    public string Weather { get; set; }
    public string TemperatureRange { get; set; }
}
public class SendEmailInput
{
    [Description("目标邮件地址")]
    public string TargetEmail { get; set; }
    [Description("邮件完整内容")]
    public string Content { get; set; }
}
public class CommonOutput
{
    public string message { get; set; }
    public object data { get; set; }
    public bool Success { get; set; }
}