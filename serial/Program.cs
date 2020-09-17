using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Globalization;

namespace serial
{
    [Serializable]
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
    [Serializable]
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
            string s1 = Console.ReadLine();
            string s2 = Console.ReadLine();
            
            /*
            string s1 = "Xml";
            string s2 = "<Input><K>10</K><Sums><decimal>1.01</decimal><decimal>2.02</decimal></Sums><Muls><int>1</int><int>4</int></Muls></Input>";//Console.ReadLine();
            */
            /*
            string s1 = "Json";
            string s2 = "{\"K\":10,\"Sums\":[1.01,2.02],\"Muls\":[1,4]}";
            */
            if (String.Compare(s1, "Xml", true) == 0)
            {
                Input input;
                //десериализация входных данных
                XmlSerializer formatteri = new XmlSerializer(typeof(Input));  
                MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(s2));
                input = (Input)formatteri.Deserialize(ms);
                //их обработка
                Output output = process(input);
                //сериализация результата
                MemoryStream mso = new MemoryStream();
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;
                XmlWriter writer = XmlWriter.Create(mso, settings);
                XmlSerializerNamespaces names = new XmlSerializerNamespaces();
                names.Add("", "");
                XmlSerializer formattero = new XmlSerializer(typeof(Output));
                formattero.Serialize(writer, output, names);
                mso.Flush();
                mso.Position = 0;
                StreamReader sr = new StreamReader(mso);
                string r = sr.ReadToEnd();  //получение сериализованного объекта
                Console.WriteLine(r);
            }
            if (String.Compare(s1, "Json", true) == 0)
            {
                Input input;
                //десериализация входных данных
                DataContractJsonSerializer formatteri = new DataContractJsonSerializer(typeof(Input));
                MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(s2));
                input = (Input)formatteri.ReadObject(ms);
                //их обработка
                Output output = process(input);
                //сериализация результата вручную, встроенные средства выдают результат немного не в том виде, что в примере
                Console.WriteLine(MyJsonSerialize(output));

            }
            Console.ReadKey();
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
            //MulResult произведение всех чисел из массива Muls входного обекта.

            output.MulResult = 1;
            foreach (int i in input.Muls)
            {
                output.SortedInputs[idx] = i;
                idx++;
                output.MulResult *= i;
            }
            //SortedInputs отсортированные числа из полей Sums, Muls входного объекта
            // сортировка массива
            Array.Sort(output.SortedInputs);
            return output;
        }
    }
}
