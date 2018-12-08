#!/bin/sh
set -e
cd `dirname $0`
VERSION=$1
if [ -z "$VERSION" ]; then
	echo "Missing version on command line!" >&2
	exit 1
fi

#for PROJECT in Activout.DatabaseClient/Activout.DatabaseClient.csproj Activout.DatabaseClient/Activout.DatabaseClient.csproj

pack()
{
	_CSPROJ="$1"
	_TITLE="$2"
	_DESCRIPTION="$3"

	dotnet pack \
		-p:Title="$_TITLE" \
		-p:Description="$_DESCRIPTION" \
		-p:PackageVersion="$VERSION" \
		-p:PackageLicenseExpression="MIT" \
		-p:PackageProjectUrl="https://github.com/twogood/Activout.DatabaseClient" \
		-p:RepositoryType="git" \
		-p:RepositoryUrl="https://github.com/twogood/Activout.DatabaseClient.git" \
		--configuration=Release \
		--include-symbols \
		--include-source \
		"$_CSPROJ"
}

pack "Activout.DatabaseClient/Activout.DatabaseClient.csproj" \
	"Activout Database Client" \
	"Create a Database Access Object (DAO) defining the C# interface you want and writing the SQL query."
pack "Activout.DatabaseClient.Dapper/Activout.DatabaseClient.Dapper.csproj" \
	"Activout Database Client - Dapper backend" \
	"Dapper Backend for Activout.DatabaseClient."

