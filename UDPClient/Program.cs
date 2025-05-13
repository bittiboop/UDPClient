using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Linq;
using Timer = System.Timers.Timer;

namespace UDPServer
{
    class Program
    {
        static Dictionary<string, decimal> computerComponents = new Dictionary<string, decimal>
        {
            { "процесор intel i5", 5000.00m },
            { "процесор intel i7", 8000.00m },
            { "процесор intel i9", 12000.00m },
            { "процесор amd ryzen 5", 4500.00m },
            { "процесор amd ryzen 7", 7500.00m },
            { "відеокарта rtx 3060", 12000.00m },
            { "відеокарта rtx 3070", 18000.00m },
            { "відеокарта rtx 3080", 25000.00m },
            { "відеокарта rtx 4080", 35000.00m },
            { "оперативна пам'ять 8gb", 1500.00m },
            { "оперативна пам'ять 16gb", 2800.00m },
            { "оперативна пам'ять 32gb", 5000.00m },
            { "жорсткий диск 1tb", 1200.00m },
            { "жорсткий диск 2tb", 2000.00m },
            { "ssd 500gb", 2000.00m },
            { "ssd 1tb", 3500.00m }
        };

        class ClientActivity
        {
            public DateTime LastActivity { get; set; }
            public int RequestCount { get; set; }
            public DateTime CounterResetTime { get; set; }
        }

        static Dictionary<string, ClientActivity> clientsActivity = new Dictionary<string, ClientActivity>();

        const int MAX_CLIENTS = 100;
        const int MAX_REQUESTS_PER_HOUR = 10;
        const int INACTIVITY_TIMEOUT_MINUTES = 10;

        static void Main(string[] args)
        {
            UdpClient server = new UdpClient(5555);
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("UDP сервер запущено на порті 5555");
            Console.WriteLine("Очікування запитів від клієнтів...");

            Timer inactivityTimer = new Timer(30000); // перевірка кожні 30 секунд
            inactivityTimer.Elapsed += CheckInactiveClients;
            inactivityTimer.AutoReset = true;
            inactivityTimer.Enabled = true;

            while (true)
            {
                try
                {
                    IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = server.Receive(ref clientEndPoint);
                    string request = Encoding.UTF8.GetString(receivedBytes);
                    string clientKey = clientEndPoint.ToString();

                    Console.WriteLine($"Отримано запит від {clientEndPoint}: {request}");

                    string limitMessage = CheckClientLimits(clientKey);
                    
                    if (!string.IsNullOrEmpty(limitMessage))
                    {
                        // Якщо клієнт перевищив ліміт, надсилаємо повідомлення про обмеження
                        byte[] limitResponse = Encoding.UTF8.GetBytes(limitMessage);
                        server.Send(limitResponse, limitResponse.Length, clientEndPoint);
                        Console.WriteLine($"Обмеження для клієнта {clientEndPoint}: {limitMessage}");
                        continue;
                    }

                    string response = ProcessRequest(request);

                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    server.Send(responseBytes, responseBytes.Length, clientEndPoint);

                    Console.WriteLine($"Надіслано відповідь: {response}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка: {ex.Message}");
                }
            }
        }

        static string CheckClientLimits(string clientKey)
        {
            DateTime now = DateTime.Now;

            if (!clientsActivity.ContainsKey(clientKey))
            {
                if (clientsActivity.Count >= MAX_CLIENTS)
                {
                    return "Сервер досяг максимальної кількості підключень. Спробуйте пізніше.";
                }

                clientsActivity[clientKey] = new ClientActivity
                {
                    LastActivity = now,
                    RequestCount = 1,
                    CounterResetTime = now.AddHours(1)
                };
                return null;
            }

            // Оновлення часу останньої активності
            ClientActivity activity = clientsActivity[clientKey];
            activity.LastActivity = now;

            if (now > activity.CounterResetTime)
            {
                activity.RequestCount = 1;
                activity.CounterResetTime = now.AddHours(1);
            }
            else
            {
                if (activity.RequestCount >= MAX_REQUESTS_PER_HOUR)
                {
                    TimeSpan timeLeft = activity.CounterResetTime - now;
                    return $"Перевищено ліміт запитів. Спробуйте через {timeLeft.Minutes} хвилин і {timeLeft.Seconds} секунд.";
                }
                activity.RequestCount++;
            }

            return null;
        }

        // Перевірка неактивних клієнтів
        static void CheckInactiveClients(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            List<string> clientsToRemove = new List<string>();

            foreach (var client in clientsActivity)
            {
                TimeSpan inactiveTime = now - client.Value.LastActivity;
                if (inactiveTime.TotalMinutes >= INACTIVITY_TIMEOUT_MINUTES)
                {
                    clientsToRemove.Add(client.Key);
                }
            }

            foreach (string clientKey in clientsToRemove)
            {
                clientsActivity.Remove(clientKey);
                Console.WriteLine($"Клієнт {clientKey} відключений через неактивність");
            }
        }

        // Метод для обробки запиту та формування відповіді
        static string ProcessRequest(string request)
        {
            string normalizedRequest = request.ToLower().Trim();

            if (normalizedRequest.StartsWith("ціна"))
            {
                string componentName = normalizedRequest.Substring(4).Trim();

                foreach (var component in computerComponents)
                {
                    if (component.Key.Contains(componentName) || componentName.Contains(component.Key))
                    {
                        return $"Ціна на {component.Key}: {component.Value:N2} грн";
                    }
                }

                return "Товар не знайдено в базі даних";
            }
            else if (normalizedRequest == "список" || normalizedRequest == "list")
            {
                StringBuilder sb = new StringBuilder("Доступні компоненти:\n");
                foreach (var component in computerComponents)
                {
                    sb.AppendLine($"- {component.Key}: {component.Value:N2} грн");
                }
                return sb.ToString();
            }
            
            return "Невідомий запит. Використовуйте формат: 'ціна [назва товару]' або 'список'";
        }
    }
}