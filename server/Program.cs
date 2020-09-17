using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Globalization;

namespace server
{
    [DataContract]
    public class Input
    {
        [DataMember]
        public int K { get; set; }
        [DataMember]
        public decimal[] Sums { get; set; }
        [DataMember]
        public int[] Muls { get; set; }
    }
    [DataContract]
    public class Output
    {
        [DataMember]
        public decimal SumResult { get; set; }
        [DataMember]
        public int MulResult { get; set; }
        [DataMember]
        public decimal[] SortedInputs { get; set; }
    }

    class Program
    {
        static HttpListener server; //слушатель запросов
        static bool bStop = false;

        //преобразование числа в строку минимум с одним знаком после точки
        static string FormatDecimal(decimal a)
        {
            string r;
            decimal d = a - Decimal.Truncate(a);
            r = a.ToString("G", CultureInfo.InvariantCulture);
            if (d == 0)
            {
                r = r + ".0";
            }
            return r;
        }
        //сериализация объекта
        static string MyJsonSerialize(Output output)
        {
            //сериализация массива
            StringBuilder arr = new StringBuilder();
            for (int i = 0; i < output.SortedInputs.Length; i++)
            {
                arr.Append(FormatDecimal(output.SortedInputs[i]));
                if (i != output.SortedInputs.Length - 1)
                    arr.Append(",");
            }
            return "{" + String.Format("\"SumResult\":{0},\"MulResult\":{1},\"SortedInputs\":[{2}]", FormatDecimal(output.SumResult), output.MulResult, arr.ToString()) + "}";
        }
        //обработка данных
        static Output process(Input input)
        {
            Output output = new Output();
            //сумма всех чисел из массива Sums входного объекта, умноженная на коэффициент K
            output.SumResult = 0;
            output.SortedInputs = new decimal[input.Muls.Length + input.Sums.Length];
            int idx = 0;
            foreach (decimal i in input.Sums)
            {
                output.SortedInputs[idx] = i;
                idx++;
                output.SumResult += i;
            }
            //умножаем сумму на К
            output.SumResult *= input.K;
            //MulResult произведение всех чисел из массива Muls входного обекта
            output.MulResult = 1;
            foreach (int i in input.Muls)
            {
                output.SortedInputs[idx] = i;
                idx++;
                output.MulResult *= i;
            }
            //SortedInputs отсортированные числа из полей Sums, Muls входного объекта
            //сортировка массива
            Array.Sort(output.SortedInputs);
            return output;
        }

        static void Main(string[] args)
        {
            //port – целое число, приходит первой строкой со стандартного потока ввода
            int port = Int32.Parse(Console.ReadLine());

            Input input;
            Output output = null;
            server = new HttpListener(); // Создаем "слушателя" для указанного порта
            server.Prefixes.Add(String.Format("http://127.0.0.1:{0}/Ping/", port));
            server.Prefixes.Add(String.Format("http://127.0.0.1:{0}/PostInputData/", port));
            server.Prefixes.Add(String.Format("http://127.0.0.1:{0}/GetAnswer/", port));
            server.Prefixes.Add(String.Format("http://127.0.0.1:{0}/Stop/", port));
            server.Start(); // Запускаем его


            // В бесконечном цикле
            while (!bStop)
            {
                //ожидаем входящие запросы
                HttpListenerContext context = server.GetContext();
                //получаем входящий запрос
                HttpListenerRequest request = context.Request;
                /*Метод пинг служит признаком того, что сервер находится в рабочем состоянии, в ответе запроса приходит
HttpStatusCode.Ok (200). В любом другом случае сервер считается недоступным.*/
                if (request.RawUrl.ToUpper().Contains("PING"))
                {
                    string responseString = "";
                    HttpListenerResponse response = context.Response;
                    response.ContentType = "text/plain; charset=UTF-8";
                    response.StatusCode = 200;
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    using (Stream o = response.OutputStream)
                    {
                        o.Write(buffer, 0, buffer.Length);
                    }
                }
                /*С помощью этого метода сервер должен безопасно закончить свою работу, тем самым закончив исполнение
программы решения участника.*/
                else if (request.RawUrl.ToUpper().Contains("STOP"))
                {
                    string responseString = "";
                    HttpListenerResponse response = context.Response;
                    response.ContentType = "text/plain; charset=UTF-8";
                    response.StatusCode = 200;
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    using (Stream o = response.OutputStream)
                    {
                        o.Write(buffer, 0, buffer.Length);
                    }
                    bStop = true;
                }
                /*С помощью этого метода программа жюри посылает входные данные для задачи. Входные данные приходят
в теле запроса в виде сериализованного в Json объекта типа Input в кодировке Utf-8.*/
                else if (request.RawUrl.ToUpper().Contains("POSTINPUTDATA"))
                {
                    string s2;
                    using (Stream body = request.InputStream)
                    {
                        using (StreamReader reader = new StreamReader(body))
                        {
                            s2 = reader.ReadToEnd();
                        }
                    }
                    //десериализация входных данных
                    DataContractJsonSerializer formatteri = new DataContractJsonSerializer(typeof(Input));
                    MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(s2));
                    input = (Input)formatteri.ReadObject(ms);
                    //их обработка
                    output = process(input);
                    string responseString = "";
                    HttpListenerResponse response = context.Response;
                    response.ContentType = "text/plain; charset=UTF-8";
                    response.StatusCode = 200;
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    using (Stream o = response.OutputStream)
                    {
                        o.Write(buffer, 0, buffer.Length);
                    }
                }
                /*С помощью этого метода программа жюри запрашивает ответ задачи. Решение нужно отдавать в теле ответа
в виде сериализованного объекта Output в Json в кодировке Utf-8.*/
                else if (request.RawUrl.ToUpper().Contains("GETANSWER"))
                {
                    string responseString;
                    HttpListenerResponse response = context.Response;
                    response.ContentType = "text/plain; charset=UTF-8";
                    if (output != null)
                    {
                        //сериализация результата 
                        responseString = MyJsonSerialize(output);
                        response.StatusCode = 200;
                    }
                    else
                    {
                        responseString = "";
                        response.StatusCode = 403;
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    using (Stream o = response.OutputStream)
                    {
                        o.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            server.Stop();
        }
    }
}
