using System;
using System.Threading;
using System.Net;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Globalization;

namespace client
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
        static void Main(string[] args)
        {
            int port = Int32.Parse(Console.ReadLine());
            Input input;
            bool ready = false;
            while (!ready)
            {
                ready = Ping(port) == HttpStatusCode.OK;
                Thread.Sleep(100);
            }
            string s2 = GetInputData(port);
            //десериализация входных данных
            DataContractJsonSerializer formatteri = new DataContractJsonSerializer(typeof(Input));
            MemoryStream ms = new MemoryStream(Encoding.GetEncoding("UTF-8").GetBytes(s2));
            input = (Input)formatteri.ReadObject(ms);
            //их обработка
            Output output = process(input);
            //сериализация
            string s3 = MyJsonSerialize(output);
            //и отправка серверу
            WriteAnswer(port, s3);
        }
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
        //отправка запроса на сервер
        static string Send(string url, string method, string contenttype, byte[] content, out HttpStatusCode code)
        {
            string useragent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
            string accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            string AcceptCharset = "Accept-Charset=windows-1251,utf-8;q=0.7,*;q=0.7";
            string AcceptLanguage = "ru-ru,ru;q=0.8,en-us;q=0.5,en;q=0.3";

            string txt = "";
            try
            {
                Uri uri = new Uri(url);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                req.ContentType = @"text/html; charset=utf-8";

                req.AllowAutoRedirect = true;
                req.UserAgent = useragent;
                req.Accept = accept;
                req.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                req.Headers.Add(HttpRequestHeader.AcceptLanguage, AcceptLanguage);
                req.Headers.Add(HttpRequestHeader.AcceptCharset, AcceptCharset);
                //Для POST-запроса
                if (method == "POST")
                {
                    req.Method = method;
                    req.ContentType = contenttype;
                    req.ContentLength = content.Length;
                    req.GetRequestStream().Write(content, 0, content.Length);
                }
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.Default);
                txt = reader.ReadToEnd();
                reader.Close();
                code = resp.StatusCode;
            }
            catch (Exception ex)
            {
                code = HttpStatusCode.InternalServerError;
            }
            return txt;
        }
        //посылка GET-запроса
        static string Get(string url, out HttpStatusCode code)
        {
            return Send(url, "GET", "", new byte[0], out code);
        }
        //посылка POST-запроса
        static string Post(string url, string postdata, out HttpStatusCode code)
        {
            byte[] ByteArr = Encoding.GetEncoding("UTF-8").GetBytes(postdata);  //кодирование отправляемых данных
            return Send(url, "POST", "application/x-www-form-urlencoded", ByteArr, out code);
        }
        /*Метод пинг служит признаком того, что сервер находится в рабочем состоянии, в ответе запроса приходит
HttpStatusCode.Ok (200). В любом другом случае сервер считается недоступным.*/
        static HttpStatusCode Ping(int port)
        {
            HttpStatusCode r;
            string url = String.Format("http://127.0.0.1:{0}/Ping", port);
            Get(url, out r);
            return r;
        }
        /*С помощью этого метода участник может получить входные данные для задачи. Входные данные приходят
в теле ответа в виде сериализованного в Json объекта типа Input в кодировке Utf-8.*/
        static string GetInputData(int port)
        {
            HttpStatusCode r;
            string url = String.Format("http://127.0.0.1:{0}/GetInputData", port);
            return Get(url, out r);
        }
        /*С помощью этого метода можно отдать ответ задачи серверу. Ответ нужно отдавать в теле запроса в виде
сериализованного объекта Output в Json в кодировке Utf-8.*/
        static HttpStatusCode WriteAnswer(int port, string data)
        {
            HttpStatusCode r;
            string url = String.Format("http://127.0.0.1:{0}/WriteAnswer", port);
            Post(url, data, out r);
            return r;
        }

    }
}
