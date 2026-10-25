
using global::Android.App;
using global::Android.Content;
using global::Android.OS;
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
        if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
            return;

        if (callDetails.CallDirection != CallDirection.Incoming)
            return;

        try
        {
            var handle = callDetails.GetHandle();
            var number = handle?.SchemeSpecificPart;

            string? series = null;
            bool shouldBlock = false;

            if (!string.IsNullOrWhiteSpace(number))
            {
                series =
                    global::HelloWorld.MainPage
                        .GetIndianFourDigitSeries(number);

                if (series is not null)
                {
                    var decisions =
                        global::HelloWorld.MainPage.LoadSeriesDecisions();

                    shouldBlock =
                        decisions.TryGetValue(series, out var decision)
                        && decision == "accepted";
                }
            }

            // Tell Android whether this call should be allowed or rejected.
            var response = new CallResponse.Builder()
                .SetDisallowCall(shouldBlock)
                .SetRejectCall(shouldBlock)
                .SetSilenceCall(false)
                .SetSkipCallLog(false)
                .SetSkipNotification(false)
                .Build();

            RespondToCall(callDetails, response);

            // Keep capturing incoming numbers, including blocked calls
            // when Android provides their numbers to this service.
            if (!string.IsNullOrWhiteSpace(number))
            {
                var record = new CallRecord
                {
                    PhoneNumber = number,
                    CapturedAtUtc = DateTimeOffset.UtcNow
                };

                CallStorage.Save(record);

                global::Android.Util.Log.Info(
                    "CallCapture",
                    shouldBlock
                        ? $"Blocked incoming number {number}, series {series}."
                        : $"Allowed incoming number {number}.");
            }
            else
            {
                global::Android.Util.Log.Info(
                    "CallCapture",
                    "Incoming call did not provide a caller number.");
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error(
                "CallCapture",
                $"Failed to screen incoming call: {ex}");

            // If screening fails, allow the call rather than accidentally
            // rejecting a caller because of an application error.
            try
            {
                var fallbackResponse = new CallResponse.Builder()
                    .SetDisallowCall(false)
                    .SetRejectCall(false)
                    .SetSilenceCall(false)
                    .SetSkipCallLog(false)
                    .SetSkipNotification(false)
                    .Build();

                RespondToCall(callDetails, fallbackResponse);
            }
            catch (Exception fallbackEx)
            {
                global::Android.Util.Log.Error(
                    "CallCapture",
                    $"Failed to send fallback call response: {fallbackEx}");
            }
        }
    }
}
