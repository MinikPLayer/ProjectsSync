#!/bin/bash

SCRIPT_NAME=${0##*/}
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
SCRIPT_PATH=$SCRIPT_DIR/$SCRIPT_NAME
INSTALL_DIR="/usr/share/pcsync"
EXEC_NAME="pcsync"

if [ ! -e $SCRIPT_PATH/$EXEC_NAME ]; then
	echo "Script must be placed in the target folder."
	echo "(Executable file $EXEC_NAME not found)"
	exit 2
fi

# Check if run as sudo
if [ "$EUID" -ne 0 ]; then
  pkexec $SCRIPT_PATH/$SCRIPT_NAME
  exit
fi

if [ -d $INSTALL_DIR ]; then
	read -p "Install directory $INSTALL_DIR already exists. Do you want to continue? (Installer will delete $INSTALL_DIR) " -n 1 -r
	echo    # (optional) move to a new line
	if [[ ! $REPLY =~ ^[Yy]$ ]]
	then
	    echo "Exiting..."
	    exit 1
	fi

	echo "Removing old directory and files..."
	rm -rf $INSTALL_DIR/*
fi

echo "Creating target directory..."
mkdir -p $INSTALL_DIR

echo "Copying files..."
rsync -av $SOURCE_DIR/* $INSTALL_DIR/ --exclude=install.sh

echo "Creating soft link..."
EXEC_TARGET_PATH="/usr/bin/$EXEC_NAME"
ln -s $INSTALL_DIR/$EXEC_NAME $EXEC_TARGET_PATH

echo "Setting executable bit..."
chmod +x $EXEC_TARGET_PATH

echo "Installation complete!"