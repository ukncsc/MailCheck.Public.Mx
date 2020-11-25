FROM mcr.microsoft.com/dotnet/core/runtime:2.1-bionic
ARG publish_dir

WORKDIR /app
COPY $publish_dir .

ENTRYPOINT ["dotnet", "MailCheck.Mx.Migration.dll"]
