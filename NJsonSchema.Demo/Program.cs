﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Jsdl.CodeGeneration;
using Jsdl.CodeGeneration.Generators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Demo
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.BufferHeight = 2000;

            var service = new JsdlService();
            service.Name = "DataService";
            service.Operations.Add(new JsdlOperation
            {
                Name = "Foo", 
                Target = "api/Sum/{0}/{1}",
                Method = JsdlOperationMethod.Delete,
                Parameters = new List<JsdlParameter>
                {
                    new JsdlParameter
                    {
                        Name = "a", 
                        ParameterType = JsdlParameterType.segment,
                        SegmentPosition = 0, 
                        Type = JsonObjectType.Integer
                    }, 
                    new JsdlParameter
                    {
                        Name = "b", 
                        ParameterType = JsdlParameterType.segment,
                        SegmentPosition = 1, 
                        Type = JsonObjectType.Integer
                    }, 
                },
                Returns = new JsonSchema4
                {
                    Type = JsonObjectType.Integer,
                }
            });

            var generator = new CSharpJsdlServiceGenerator(service);
            generator.Namespace = "Test";
            var code = generator.GenerateFile();

            Console.WriteLine(code);
            Console.ReadLine();



            var passes = 0;
            var fails = 0;
            var exceptions = 0;
            var files = Directory.GetFiles("Tests");
            foreach (var file in files)
            {
                Console.WriteLine("File: " + file);
                var data = JArray.Parse(File.ReadAllText(file));
                foreach (var suite in data.OfType<JObject>())
                {
                    var description = suite["description"].Value<string>();
                    Console.WriteLine("  Suite: " + description);
                    
                    foreach (var test in suite["tests"].OfType<JObject>())
                    {
                        var testDescription = test["description"].Value<string>();
                        var valid = test["valid"].Value<bool>();

                        Console.WriteLine("    Test: " + testDescription);
                        Console.WriteLine("      Valid: " + valid);

                        //if (testDescription == "both anyOf invalid")
                            RunTest(suite, test["data"], valid, ref fails, ref passes, ref exceptions);
                    }
                }
            }

            Console.WriteLine("Passes: " + passes);
            Console.WriteLine("Fails: " + fails);
            Console.WriteLine("Exceptions: " + exceptions);

            Console.ReadLine();

            //var schema = JsonSchema4.FromType<Person>();
            //var schemaData = schema.ToJson();

            //var jsonToken = JToken.Parse("{}");
            //var errors = schema.Validate(jsonToken);

            //foreach (var error in errors)
            //    Console.WriteLine(error.Path + ": " + error.Kind);

            //schema = JsonSchema4.FromJson(schemaData);

            //Console.ReadLine();
        }

        private static void RunTest(JObject suite, JToken value, bool expectedResult, ref int fails, ref int passes, ref int exceptions)
        {
            try
            {
                var schema = JsonSchema4.FromJson(suite["schema"].ToString());
                var errors = schema.Validate(value);
                var success = expectedResult ? errors.Count == 0 : errors.Count > 0;

                if (!success)
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("      Result: " + success);

                if (!success)
                    Console.ForegroundColor = ConsoleColor.Gray;

                if (!success)
                    fails++;
                else
                    passes++;
            }
            catch (Exception ex)
            {
                exceptions++;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("      Exception: " + ex.GetType().FullName);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }

    public class Person
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public Sex Sex { get; set; }

        public DateTime Birthday { get; set; }

        public Collection<Job> Jobs { get; set; }

        [Range(2, 5)]
        public int Test { get; set; }
    }

    public class Job
    {
        public string Company { get; set; }
    }

    public enum Sex
    {
        Male,
        Female
    }
}