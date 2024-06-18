# Use the .NET SDK image for development
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev

# Set the working directory
WORKDIR /app

# Copy the project file and restore dependencies
COPY src/ChessAPI/*.csproj ./src/ChessAPI/
WORKDIR /app/src/ChessAPI/
RUN dotnet restore

# Copy the rest of the application code
COPY src/ChessAPI/. .

# Expose the port the application runs on
EXPOSE 5000

RUN dotnet dev-certs https --trust

# Set the entry point for the development server
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:5000"]
# dotnet watch run --urls https://0.0.0.0:5000
# CMD ["tail", "-f", "/dev/null"]
