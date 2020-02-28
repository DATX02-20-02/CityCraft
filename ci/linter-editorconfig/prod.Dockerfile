FROM archlinux:latest

WORKDIR /app

RUN pacman -Sy --noconfirm dotnet-sdk
RUN dotnet tool install -g dotnet-format
RUN ls /root/.dotnet/tools

CMD /root/.dotnet/tools/dotnet-format --dry-run --check -f CityPCG-unity/Assets/
