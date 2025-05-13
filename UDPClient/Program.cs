using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

        static void Main(string[] args)
        {
            UdpClient server = new UdpClient(5555);
            Console.OutputEncoding = Encoding.UTF8;
            
            Console.WriteLine("UDP сервер запущено на порті 5555");
            Console.WriteLine("Очікування запитів від клієнтів...");

            // Очікування та обробка запитів від клієнтів
            while (true)
            {
                try
                {
                    // Отримання даних від клієнта
                    IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = server.Receive(ref clientEndPoint);
                    string request = Encoding.UTF8.GetString(receivedBytes);

                    Console.WriteLine($"Отримано запит від {clientEndPoint}: {request}");

                    // Обробка запиту та підготовка відповіді
                    string response = ProcessRequest(request);

                    // Надсилання відповіді клієнту
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

        // Метод для обробки запиту та формування відповіді
        static string ProcessRequest(string request)
        {
            // Переведення запиту в нижній регістр для уникнення проблем зі співставленням
            string normalizedRequest = request.ToLower().Trim();

            // Перевірка, чи запит про ціну
            if (normalizedRequest.StartsWith("ціна"))
            {
                // Виділення назви компонента з запиту (видаляємо "ціна" на початку)
                string componentName = normalizedRequest.Substring(4).Trim();

                // Пошук компонента у базі даних
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
                // Повернення списку всіх компонентів
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