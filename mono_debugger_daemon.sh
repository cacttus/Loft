#!/bin/bash

#Run dameon

while true
do
  echo "running mono.."
  res=$(mono --debug --debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:55555 ~/git/OpenTK_Test_Linux/bin/Debug/OpenTK_Test_Linux.exe 2>&1 )

  #check if error
  result=$( echo "$res" | grep -e "debugger-agent.*Unable to listen")
  if [ -n "$result" ] ; then
   echo mono already running.
   exit 0 
  fi

done
