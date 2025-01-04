using Google.Cloud.Vision.V1;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class BananagramsGridOCR
{
    static void Main(string[] args)
    {
        // Path to your service account JSON key file
        string credentialPath = "/Users/kieran/csharp/bananagrab/subscribers-182822-a8b936070310.json"; // Replace with your actual file path
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);

        // Image path and grid parameters from Python output
        // string imagePath = "/Users/kieran/grid_overlayed_image.jpg"; // Replace with your image path
        string imagePath = "/Users/kieran/bgrid1_overlayed.jpg"; // Replace with your image path

        /*
            Final Grid Parameters:
            Grid Width (px): 135.95999999999998
            Grid Height (px): 135.95999999999998
            X Offset (px): -35
            Y Offset (px): -30
            Rows: 25, Cols: 25
        */
        int gridSize = 25; // Number of rows and columns (15x15 for Bananagrams)
        float gridWidth = 135.95999999999998f; // Grid cell width in pixels
        float gridHeight = 135.95999999999998f; // Grid cell height in pixels
        int xOffset = -35; // X offset in pixels
        int yOffset = -30; // Y offset in pixels
        /*
        // int gridSize = 15; // Number of rows and columns (15x15 for Bananagrams)
        // float gridWidth = 89.8f; // Grid cell width in pixels
        // float gridHeight = 91.93f; // Grid cell height in pixels
        // int xOffset = -20; // X offset in pixels
        // int yOffset = -70; // Y offset in pixels
        */

        // Step 1: Generate grid cell dimensions
        List<RectangleF> gridCells = GenerateGrid(gridSize, gridWidth, gridHeight, xOffset, yOffset);

        // Step 2: Perform OCR on each cell
        Dictionary<(int, int), string> detectedLetters = DetectLettersInGrid(imagePath, gridCells);

        // Step 3: Display the detected letters in a grid format
        DisplayDetectedGrid(detectedLetters, gridSize);
    }

    static List<RectangleF> GenerateGrid(int gridSize, float gridWidth, float gridHeight, int xOffset, int yOffset)
    {
        // Generate rectangles for each grid cell
        var gridCells = new List<RectangleF>();
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                gridCells.Add(new RectangleF(
                    col * gridWidth + xOffset,
                    row * gridHeight + yOffset,
                    gridWidth,
                    gridHeight));
            }
        }
        return gridCells;
    }

    static Dictionary<(int, int), string> DetectLettersInGrid(string imagePath, List<RectangleF> gridCells)
    {
        var client = ImageAnnotatorClient.Create();
        var detectedLetters = new Dictionary<(int, int), string>();

        // Load the image and get its dimensions
        using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath))
        {
            int imageWidth = image.Width;
            int imageHeight = image.Height;

            foreach (var (cell, index) in gridCells.Select((value, i) => (value, i)))
            {
                // Convert RectangleF to Rectangle and constrain it within image bounds
                var rectangle = new SixLabors.ImageSharp.Rectangle(
                    Math.Max(0, (int)cell.X),
                    Math.Max(0, (int)cell.Y),
                    Math.Min((int)cell.Width, imageWidth - (int)cell.X),
                    Math.Min((int)cell.Height, imageHeight - (int)cell.Y)
                );

                // Skip invalid rectangles (e.g., width or height <= 0)
                if (rectangle.Width <= 0 || rectangle.Height <= 0)
                {
                    detectedLetters[(index / 25, index % 25)] = " ";
                    continue;
                }

                using (var cellImage = image.Clone(ctx => ctx.Crop(rectangle)))
                {
                    // Preprocess the cropped cell: convert to grayscale and enhance contrast
                    cellImage.Mutate(ctx =>
                    {
                        ctx.Grayscale(); // Convert to grayscale
                        ctx.Contrast(1.2f); // Enhance contrast
                    });

                    using (var ms = new MemoryStream())
                    {
                        cellImage.SaveAsJpeg(ms);
                        var cellBytes = ms.ToArray();

                        // Perform OCR on the preprocessed cell image
                        var response = client.DetectText(Google.Cloud.Vision.V1.Image.FromBytes(cellBytes));
                        if (response.Count > 0)
                        {
                            detectedLetters[(index / 25, index % 25)] = response.First().Description.Trim();
                        }
                        else
                        {
                            detectedLetters[(index / 25, index % 25)] = " "; // Empty space if no text detected
                        }
                    }
                }
            }
        }

        return detectedLetters;
    }

    static void DisplayDetectedGrid(Dictionary<(int, int), string> detectedLetters, int gridSize)
    {
        Console.WriteLine("Detected Letters Grid:");

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                if (detectedLetters.TryGetValue((row, col), out var letter))
                {
                    Console.Write(letter.PadRight(2)); // Print each letter with spacing
                }
                else
                {
                    Console.Write("  "); // Print space for missing letters
                }
            }
            Console.WriteLine(); // New line for the next row
        }
    }
}