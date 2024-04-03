using System;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Net.Http;
using Xamarin.Essentials;
using Newtonsoft.Json;
using System.Threading;



public class ApiResponse
{
    //define class to handle the response from the rotating code API
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
    //Initiate the code refresh class
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
    //Function for initial code request from API when app starts.
    public void StartService()
    {
        RefreshCode(null);
    }
    //Request code from REST Endpoint
    private async void RefreshCode(object state)
    {
        try
        {
            string codeAPIendpoint = "https://pz6392ttu9.execute-api.eu-north-1.amazonaws.com/development"; //REST API endpoint
            //Initiate new httpClientService for the local function.
            HttpClientService httpClientService = new HttpClientService();
            //Set json payload
            var postData = new
            {
                METHOD = "GET"
            };
            //Use await process to call the class "httpClientService" along with the PostDataAsync task found within the class.
            string response = await httpClientService.PostDataAsync(postData, codeAPIendpoint);
            Console.WriteLine(response);
            //If response is not null continue, or else catch the exception.
            if (!string.IsNullOrEmpty(response))
            {
                //Deserialize the json response from the API
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);
                //Validate whether the JSON response is correct and contains a 200 status code.
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
            // Complete exception handling in future.
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
    
    //define GET Data Task
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
    //define POST data Task
    public async Task<string> PostDataAsync(object data, string endpoint)
    {
        try
        {
            string jsonData = JsonConvert.SerializeObject(data);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            //await response from POST request
            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
            //handle API response
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseContent);
                return responseContent;
            }
            else
            {
                Console.WriteLine("Error Retrieving data from api");
                return null;
            }
        }
        catch (Exception ex)
        {
            return null;
        }
    }

}


namespace Final_Year_Project_App
{
    public partial class MainPage : ContentPage
    {
        private const int SAMPLING_TIME = 50; // Adjust this value depending upon the sampling rate of the receiver
        private string text { get; set; } = "1234"; //set code to 1234 upon app start
        //
        public void UpdateCode(int newCode)
        {
            text = newCode.ToString();
            Device.BeginInvokeOnMainThread(() =>
            {
                currentCode.Text = newCode.ToString();
            });
        }

        private CodeRefreshService codeRefreshService;

        public MainPage()
        {
            //App start initialisation
            InitializeComponent();

            codeRefreshService = new CodeRefreshService();
            codeRefreshService.OnCodeUpdated = UpdateCode; 
            codeRefreshService.StartService();

        }


        private void StartTransmission2(int pin)
        {
            //convert PIN which is parsed in to 4 bit binary
            string binaryRepresentation = "";
            foreach (char digit in pin.ToString())
            {
                int digitValue = int.Parse(digit.ToString());
                string binaryDigit = Convert.ToString(digitValue, 2).PadLeft(4, '0');
                binaryRepresentation += binaryDigit;
            }
            TransmitByte2(binaryRepresentation);
        }

        private async void TransmitByte2(string binaryRepresentation)
        {
            //begin transmission of the binary representation of the active PIN code
            var watch = new System.Diagnostics.Stopwatch();
            Console.WriteLine(binaryRepresentation);
            watch.Start();
            //Initiate start sequence of 1,0 at the start of every transmission
            await Flashlight.TurnOnAsync();
            await Task.Delay(SAMPLING_TIME);
            await Flashlight.TurnOffAsync();
            await Task.Delay(SAMPLING_TIME);
            for (int i=0; i < binaryRepresentation.Length; i++)
            {
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
            }
            // returning to IDLE state
            await Flashlight.TurnOffAsync();
        }

        private void transmitButton_Clicked(object sender, EventArgs e)
        {
            //When transmit button is clicked, call function to begin transmission.
            int pin = int.Parse(text);
            StartTransmission2(pin);
        }
    }
}
