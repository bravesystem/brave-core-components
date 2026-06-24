# escape=`
# Match this to your host OS family (LTSC 2022 shown here)
FROM mcr.microsoft.com/dotnet/framework/runtime:4.8-windowsservercore-ltsc2022

# Set working directory
WORKDIR C:/app/bin/x64/Debug

# Copy files from bin/x64/Debug
COPY bin/x64/Debug/ .

# Copy files from Win64_x64 into the same directory
COPY Win64_x64/ .

ENTRYPOINT ["BRaVe-Biometric-Matching-Webjob.exe"]