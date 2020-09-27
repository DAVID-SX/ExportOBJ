using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleTest
{
    class Program
    {
        public static void Main(string[] args)
        {
            string path = "混凝土 - 现场浇注混凝土";
            Console.WriteLine(path.Replace(" ", "_"));
            Console.ReadKey();
        }
    }
}
