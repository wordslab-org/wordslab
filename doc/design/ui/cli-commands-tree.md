# Commands

Syntax:

wordslab [object] [verb] -[option_name] [option_value]

[xxx]vm
- status
- create
- update
- config
  - get
  - set
- gpu
  - enable
  - disable
- delete
- start
- stop
- cluster
  - status
  - imageinfo
- execenv
  - create
  - delete
  - status
  - config

localvm
[same as xxxvm]
- hostinfo

cloudvm
[same as xxxvm]
- list
- billing

execenv
- connect
- disconnect
- list
- status
- module
  - install
  - update
  - uninstall
  - list
  - status
  - config

backup
- -vm | -execenv
- list
- create
- delete
- restore
- schedule
  - create
  - config
  - delete
