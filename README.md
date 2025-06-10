# Cover Letter Generator

A web application built with ASP.NET Core that generates professional cover letters using OpenAI. Upload a job description and your CV, and the app produces a tailored letter that can be downloaded as a Word document.

## Features
- Upload job description and CV in PDF or DOCX format.
- Uses OpenAI's GPT model to craft a personalized cover letter.
- Preview the generated letter and download it in Word format.

## Requirements
- [.NET 8.0 SDK](https://dotnet.microsoft.com/)
- An OpenAI API key

## Getting Started
1. Clone this repository.
2. Create a `.env` file in the project root containing your API key:
   ```
   OPENAI_API_KEY=your-openai-key
   ```
3. Restore dependencies and run the application:
   ```bash
   dotnet run --project CoverLetterGenerator
   ```
4. Open `https://localhost:5001` (or `http://localhost:5000`) in your browser.

## Project Layout
- `Controllers/` – MVC controllers handling requests and document generation.
- `Models/` – request and response models for the cover letter workflow.
- `Services/` – service that calls the OpenAI API.
- `Views/` – Razor views for the UI.
- `Templates/` – Word document template used for downloads.
