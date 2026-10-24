
using System.Text.Json;
using Android.App;
using Android.Content;

namespace HelloWorld.Platforms.Android;

public static class CallStorage
{
    private const string StoreName = "CapturedCallStore";
    private const string CallsKey = "CapturedCalls";

    public static void Save(global::HelloWorld.CallRecord record)
    {
        var preferences = global::Android.App.Application.Context.GetSharedPreferences(
            StoreName, FileCreationMode.Private);

        var json = preferences?.GetString(CallsKey, "[]") ?? "[]";

        var calls =
            JsonSerializer.Deserialize<List<global::HelloWorld.CallRecord>>(json)
            ?? new List<global::HelloWorld.CallRecord>();

        calls.Insert(0, record);

        var updatedJson = JsonSerializer.Serialize(calls.Take(200).ToList());

        var editor = preferences?.Edit()
            ?? throw new InvalidOperationException(
                "Unable to open local call storage.");

        editor.PutString(CallsKey, updatedJson);
        editor.Apply();
    }

    public static List<global::HelloWorld.CallRecord> Load()
    {
        var preferences = global::Android.App.Application.Context.GetSharedPreferences(
            StoreName, FileCreationMode.Private);

        var json = preferences?.GetString(CallsKey, "[]") ?? "[]";

        return JsonSerializer.Deserialize<List<global::HelloWorld.CallRecord>>(json)
            ?? new List<global::HelloWorld.CallRecord>();
    }
}
