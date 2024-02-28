using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Net.Http;
using Xamarin.Essentials;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Threading;



public class ApiResponse
{
    public int Status { get; set; }
    public CodeData[] Data { get; set; }
}

public class CodeData
{
    public int Code { get; set; }
    public DateTime Expiry { get; set; }
}



public class CodeRefreshService
{
    private Timer timer;
    private readonly TimeSpan refreshInterval = TimeSpan.FromSeconds(30);
    private bool isInitialRequest = true;
    private HttpClientService httpClientService;

    public Action<int> OnCodeUpdated { get; set; }
    public event Action<int> CodeUpdated;
    public CodeRefreshService()
    {
        httpClientService = new HttpClientService();
    }

    public void StartService()
    {
        RefreshCode(null);
    }

    private async void RefreshCode(object state)
    {
        try
        {
            string codeAPIendpoint = "https://pz6392ttu9.execute-api.eu-north-1.amazonaws.com/development";
            HttpClientService httpClientService = new HttpClientService();
            var postData = new
            {
                METHOD = "GET"
            };
            string response = await httpClientService.PostDataAsync(postData, codeAPIendpoint);
            Console.WriteLine(response);
            if (!string.IsNullOrEmpty(response))
            {
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

                if (apiResponse.Status == 200 && apiResponse.Data != null && apiResponse.Data.Length > 0)
                {
                    var codeData = apiResponse.Data[0];

                    OnCodeUpdated?.Invoke(codeData.Code);
                    // Process the new code (codeData.Code) and expiry timestamp (codeData.Expiry)

                    if (isInitialRequest)
                    {
                        timer = new Timer(RefreshCode, null, refreshInterval, refreshInterval);
                        isInitialRequest = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions
        }
    }

    public void StopService()
    {
        timer?.Dispose();
    }
}


public class HttpClientService
{
    private readonly HttpClient _httpClient;

    public Uri BaseAddress
    {
        get { return _httpClient.BaseAddress;  }
        set { _httpClient.BaseAddress = value; }
    }
    public HttpClientService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMilliseconds(10000);
    }

    public async Task<string> GetDataAsync(string endpoint)
    {
        try
        {
            // Send a GET request
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            else
            {
                // Handle the error here
                return null;
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions here
            return null;
        }
    }

    public async Task<string> PostDataAsync(object data, string endpoint)
    {
        try
        {
            string jsonData = JsonConvert.SerializeObject(data);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseContent);
                return responseContent;
            }
            else
            {
                // Handle the error here
                Console.WriteLine("Error Retrieving data from api");
                return null;
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions here
            return null;
        }
    }

}


namespace Final_Year_Project_App
{
    class MyObject
    {
        public int code { get; set; }
        public string expiry { get; set; }
    }
    public partial class MainPage : ContentPage
    {
        private const int SAMPLING_TIME = 50; // Adjust this value according to your needs
        private string text { get; set; } = "1234";
        private string text3 = "6789";
        private const byte text2 = 1;
        private bool ledState = false;
        private bool buttonState = false;
        private bool transmitData = true;
        private int bytesCounter;
        private int totalBytes;

        public void UpdateCode(int newCode)
        {
            text = newCode.ToString();
            Device.BeginInvokeOnMainThread(() =>
            {
                currentCode.Text = newCode.ToString();
            });
            // Optionally, perform additional actions when the code is updated
        }

        private CodeRefreshService codeRefreshService;

        // Existing fields and methods...

        public MainPage()
        {
            InitializeComponent();
            //LoadData();

            codeRefreshService = new CodeRefreshService();
            codeRefreshService.OnCodeUpdated = UpdateCode; // Set the delegate
            codeRefreshService.StartService();

            // Other initialization...
        }


        private void StartTransmission2(int pin)
        {
            string binaryRepresentation = "";
            foreach (char digit in pin.ToString())
            {
                int digitValue = int.Parse(digit.ToString());
                string binaryDigit = Convert.ToString(digitValue, 2).PadLeft(4, '0');
                binaryRepresentation += binaryDigit;
            }
            TransmitByte2(binaryRepresentation);
        }



        private async void TransmitByte(char dataByte)
        {
            // Simulate turning on/off the flashlight
            //await Flashlight.TurnOnAsync();
            //await Task.Delay(SAMPLING_TIME);

            for (int i = 0; i < 4; i++)
            {
                // Simulate setting the flashlight state based on the i-th bit of dataByte
                string binary = Convert.ToString(text2, 2);
                Console.WriteLine(binary);
                if (((dataByte >> i) & 0x01) == 1)
                {
                    await Flashlight.TurnOnAsync();
                    await Flashlight.TurnOffAsync();
                }
                else
                {
                    await Flashlight.TurnOffAsync();
                }

                await Task.Delay(SAMPLING_TIME);
            }

            // Simulate returning to IDLE state
            await Flashlight.TurnOffAsync();
            await Task.Delay(SAMPLING_TIME);

        }
        private async void TransmitByte2(string binaryRepresentation)
        {
            var watch = new System.Diagnostics.Stopwatch();
            // Simulate turning on/off the flashlight
            Console.WriteLine(binaryRepresentation);
            watch.Start();
            await Flashlight.TurnOnAsync();
            //Console.WriteLine("On");
            await Task.Delay(SAMPLING_TIME);
            await Flashlight.TurnOffAsync();
            //Console.WriteLine("0");
            await Task.Delay(SAMPLING_TIME);
/*            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);*/
            //watch.Start();
            for (int i=0; i < binaryRepresentation.Length; i++)
            {
               /* var watch1 = new System.Diagnostics.Stopwatch();
                watch1.Start();*/
                //Console.WriteLine(binaryRepresentation[i]);
                if (binaryRepresentation[i] == '1')
                {
                    await Flashlight.TurnOnAsync();
                    await Task.Delay(SAMPLING_TIME);
                }
                else
                {
                    await Flashlight.TurnOffAsync();
                    await Task.Delay(SAMPLING_TIME);
                }
              /*  watch1.Stop();*/
                /*Console.WriteLine(watch.ElapsedMilliseconds);*/
            }
            // Simulate returning to IDLE state
            await Flashlight.TurnOffAsync();
            /*watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);*/
        }

        private void transmitButton_Clicked(object sender, EventArgs e)
        {
            int pin = int.Parse(text3);
            StartTransmission2(pin);
        }
    }
}
