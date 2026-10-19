
using System.Text.Json;
using global::Android.App;
using global::Android.Content;
using global::Android.Telecom;

namespace HelloWorld.Platforms.Android;

[Service(
    Name = "com.companyname.helloworld.IncomingCallScreeningService",
    Permission = "android.permission.BIND_SCREENING_SERVICE",
    Exported = true)]
[IntentFilter(new[] { "android.telecom.CallScreeningService" })]
public class IncomingCallScreeningService : CallScreeningService
{
    public override void OnScreenCall(Call.Details callDetails)
    {
        try
        {
            var response = new CallScreeningService.CallResponse.Builder()
                .SetDisallowCall(false)
                .SetRejectCall(false)
                .SetSilenceCall(false)
                .SetSkipCallLog(false)
                .SetSkipNotification(false)
                .Build();

            RespondToCall(callDetails, response);

            // GetHandle() avoids confusing the Android URI handle
            // with the Java object's Handle property.
            var handle = callDetails.GetHandle();
            var number = handle?.SchemeSpecificPart;

            if (string.IsNullOrWhiteSpace(number))
                return;

            _ = Task.Run(async () =>
            {
                try
                {
                    var record = new CallRecord
                    {
                        PhoneNumber = number,
                        CapturedAtUtc = DateTimeOffset.UtcNow
                    };

                    var json =
                        global::Microsoft.Maui.Storage.Preferences.Default
                            .Get("CapturedCalls", "[]");

                    var calls =
                        JsonSerializer.Deserialize<List<CallRecord>>(json)
                        ?? new List<CallRecord>();

                    calls.Insert(0, record);

                    global::Microsoft.Maui.Storage.Preferences.Default.Set(
                        "CapturedCalls",
                        JsonSerializer.Serialize(
                            calls.Take(200).ToList()));

                    await CallUploader.TryUploadAsync(record);
                }
                catch (Exception ex)
                {
                    global::Android.Util.Log.Error(
                        "CallCapture", ex.ToString());
                }
            });
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error(
                "CallCapture", ex.ToString());
        }
    }
}
