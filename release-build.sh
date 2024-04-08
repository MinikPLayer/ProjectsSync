#!/bin/bash

set -e


SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
if [ -z "${SCRIPT_DIR}" ]; then
	echo "Cannot determine script location. Exiting"
	exit 1
fi

#echo "Enter version string: "
#read VERSION
#if [ -z "$VERSION" ]; then
#	echo "Empty version string. Exiting"
#	exit 2
#fi
if [ $# = 0 ] || [ -z "$1" ]; then
	echo "Usage: ./release-build.sh <version_string>"
	exit 3
fi

VERSION=$1
BUILD_OUTPUT_DIR=$SCRIPT_DIR/build/
if [ -d $BUILD_OUTPUT_DIR ]; then
	rm -r $BUILD_OUTPUT_DIR
fi
mkdir $BUILD_OUTPUT_DIR

function build_and_pack() {
	PROJECT_DIR=$1
	PROJECT_NAME=$2
	DOTNET_VERSION=$3
	REMOVE_RUNTIMES=$4

	echo "Building $PROJECT..."
	cd $SCRIPT_DIR/$PROJECT_DIR
	dotnet clean
	dotnet build -c Release
	cd bin/Release/$DOTNET_VERSION/
	if [ $REMOVE_RUNTIMES = 1 ]; then
		echo "Removing runtimes directory..."
		rm -r ./runtimes
	fi

	ZIP_FILE_NAME=${PROJECT_NAME}_${VERSION}_linux.zip
	if [ -e $ZIP_FILE_NAME ]; then
		rm $ZIP_FILE_NAME
	fi

	zip -r ${ZIP_FILE_NAME} *
	cp $ZIP_FILE_NAME $BUILD_OUTPUT_DIR/
}

build_and_pack "PcSyncLib" "pcsync-lib" "net7.0"
build_and_pack "PcSyncLibCLI" "pcsync-cli" "net8.0" 1

