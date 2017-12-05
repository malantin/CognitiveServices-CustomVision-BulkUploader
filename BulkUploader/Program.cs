// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: https://azure.microsoft.com/en-us/services/cognitive-services
// 
// This small application is based on a sample for Microsoft Cognitive Services that you will find on GitHub:
// https://github.com/Microsoft/Cognitive-CustomVision-Windows
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Models;

namespace BulkUploader
{
    class Program
    {
        // Limit to X images
        private static int max = 100;
        // Project Name
        private static string projectName = "{Project name}";
        // Custom Vision Training Key
        private static string trainingKeyString = "{key}";

        static void Main(string[] args)
        {
            // You can either add your training key here, pass it on the command line, or type it in when the program runs
            string trainingKey = GetTrainingKey(trainingKeyString, args);

            // Create the Api, passing in a credentials object that contains the training key
            TrainingApiCredentials trainingCredentials = new TrainingApiCredentials(trainingKey);
            TrainingApi trainingApi = new TrainingApi(trainingCredentials);

            // Create a new project
            Console.WriteLine("Creating new project:");
            var project = trainingApi.CreateProject(projectName);

            // Create some tags, you need at least two
            var tag1 = trainingApi.CreateTag(project.Id, "tag1");
            var tag2 = trainingApi.CreateTag(project.Id, "tag2");

            // Add some images to the tags
            Console.Write("\n\tProcessing images");

            // Upload using the path to the images, a reference to the training API, a ference to your project and a tag
            UploadImages(@"..\..\..\Images\1", trainingApi, project, new List<string>() { tag1.Id.ToString() });
            UploadImages(@"..\..\..\Images\1", trainingApi, project, new List<string>() { tag2.Id.ToString() });

            // Or uploaded in a single batch 
            //trainingApi.CreateImagesFromData(project.Id, japaneseCherryImages, new List<Guid>() { japaneseCherryTag.Id });

            // Now there are images with tags start training the project
            Console.WriteLine("\tStarting training");

            IterationModel iteration = null;

            try
            {
                iteration = trainingApi.TrainProject(project.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Trainig could not be completed. Error: {e.Message}");
            }

            if (iteration != null)
            {
                // The returned iteration will be in progress, and can be queried periodically to see when it has completed
                while (iteration.Status == "Training")
                {
                    Thread.Sleep(1000);

                    // Re-query the iteration to get it's updated status
                    iteration = trainingApi.GetIteration(project.Id, iteration.Id);
                }

                Console.WriteLine($"\tFinished training iteration {iteration.Id}");

                // The iteration is now trained. Make it the default project endpoint
                iteration.IsDefault = true;
                trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);
                Console.WriteLine("Done!\n");

                // Now there is a trained endpoint, it can be used to make a prediction

                // Get the prediction key, which is used in place of the training key when making predictions
                //var account = trainingApi.GetAccountInfo();
                //var predictionKey = account.Keys.PredictionKeys.PrimaryKey;

                //// Create a prediction endpoint, passing in a prediction credentials object that contains the obtained prediction key
                //PredictionEndpointCredentials predictionEndpointCredentials = new PredictionEndpointCredentials(predictionKey);
                //PredictionEndpoint endpoint = new PredictionEndpoint(predictionEndpointCredentials);

                //// Make a prediction against the new project
                //Console.WriteLine("Making a prediction:");
                //var result = endpoint.PredictImage(project.Id, testImage);

                //// Loop over each prediction and write out the results
                //foreach (var c in result.Predictions)
                //{
                //    Console.WriteLine($"\t{c.Tag}: {c.Probability:P1}");
                //}
            }

            Console.ReadKey();
        }

        private static void UploadImages(String ImagePath, TrainingApi trainingApi, ProjectModel project, List<string> tags)
        {
            // Load images to upload and tag from disk
            List<MemoryStream> images = LoadImagesFromDisk(ImagePath);
            Console.Write("\tUploading");

            int count = 0;
            int uploadCounter = 0;
            int skip = 1;
            foreach (var image in images)
            {
                if (count % skip == 0)
                {
                    try
                    {
                        Console.Write(".");
                        trainingApi.CreateImagesFromData(project.Id, image, tags);
                        uploadCounter++;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"\n\tException: {e.Message}");
                        Console.Write("\n\tContinuing");
                    }
                }

                count++;

                if (count == max * skip)
                {
                    break;
                }

                //Remove image from memory
                //image.Dispose();

                // Throttle upload
                //Thread.Sleep(100);
            }

            Console.Write($"\tSuccessfully uploaded {uploadCounter} images.\n");
        }

        private static string GetTrainingKey(string trainingKey, string[] args)
        {
            if (string.IsNullOrWhiteSpace(trainingKey) || trainingKey.Equals("<your key here>"))
            {
                if (args.Length >= 1)
                {
                    trainingKey = args[0];
                }

                while (string.IsNullOrWhiteSpace(trainingKey) || trainingKey.Length != 32)
                {
                    Console.Write("Enter your training key: ");
                    trainingKey = Console.ReadLine();
                }
                Console.WriteLine();
            }

            return trainingKey;
        }

        private static List<MemoryStream> LoadImagesFromDisk(String imagePath)
        {
            // this loads the images to be uploaded from disk into memory
            Console.WriteLine($"\n\tLoading images from {imagePath} into memory.");
            return Directory.GetFiles(imagePath).Select(f => new MemoryStream(File.ReadAllBytes(f))).ToList();
        }
    }
}
