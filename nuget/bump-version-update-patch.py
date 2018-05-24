# **MODE OF USE**: just execute

############ config ############

################################

import sys, os, shutil, random, re
import __main__
import getopt
from datetime import datetime  
from datetime import timedelta  
import subprocess


def regexFindGroup1(regex, text):
	mo = re.search(regex, text)
	if mo == None:
		return None
	return mo.group(1)

#======================================================

# command line options: note the first line parameter and the if statements below
# optlist, args = getopt.getopt(sys.argv[1:], "")
# if len(args) <= 0:
# 	print("Usage: must supply last commit date as first arg")
# 	exit(1)
# lastDate = args[0]


# .... read the whole RafaelSoft.TsCodeGen.nuspec
fileNuspec = open('./nuget/RafaelSoft.TsCodeGen.nuspec', encoding="utf8")
textNuspec = fileNuspec.read()
fileNuspec.close()

# .... compute next version
curVersion = regexFindGroup1(r'<version>(.*)</version>', textNuspec)
curVersionPrefix = regexFindGroup1(r'(\d+\.\d+\.)\d+', curVersion)
curVersionLastNum = regexFindGroup1(r'\d+\.\d+\.(\d+)', curVersion)
nextVersion = curVersionPrefix + str(int(curVersionLastNum) + 1)

# .... regen patch file
filePatch = open('./nuget/RafaelSoft.TsCodeGen-bumpVersion.template.patch', encoding="utf8")
textPatch = filePatch.read()
filePatch.close()
textPatch = textPatch.replace('$NewVersion$', nextVersion)

# .... write to patch file
patchFileOut = open('./nuget/.bump-version/RafaelSoft.TsCodeGen-bumpVersion.patch', "w", encoding="utf8")	
patchFileOut.write(textPatch)
patchFileOut.close()
