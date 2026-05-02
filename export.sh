#!/bin/bash
Address="https://localhost"
Login="admin"
Password="admin"
CheckTimeout="40"
TESSA="Tessa"

Database=""
Connection="default"

get_script_dir () {
	SOURCE="${BASH_SOURCE[0]}"
	# while $SOURCE is a symlink, resolve it
	while [ -h "$SOURCE" ]; do
		DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
		SOURCE="$( readlink "$SOURCE" )"
		# if $SOURCE was a relative symlink (so no -" as prefix, need to resolve it relative to the symlink base directory
		[[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE"
	done
	DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
	echo "$DIR"
}
CurrentDir="$(get_script_dir)"

Configuration="$CurrentDir/Configuration"
Tools="$CurrentDir/tools"

exit_on_error () {
	ErrorLevel="$?"
	if [ "$ErrorLevel" != "0" ]; then
		echo
		echo "Export failed with error code: $ErrorLevel"
		echo "See the details in log file: $Tools/log.txt"
		exit 1
	fi
}

show_help() {
	echo "${TESSA^^} exporting script usage"
	echo
	echo "    Some options have aliases after comma: -short, --long"
	echo "    Options with colon have value: -a:value"
	echo "    Default value or behavior is in square brackets, used when option is omitted: example [default_value_here]"
	echo "    If any listed option is provided, then script will always run in non-interactive mode"
	echo
	echo "-d, --default                 force running script in non-interactive mode, all options are default unless specified"
	echo "-v, --verbose                 output commands results to console [commands execute in quiet mode by default]"
	echo "-a:, --address:               web service address [$Address]"
	echo "-u:, --user:                  user login to use when connecting to web service [$Login]"
	echo "-p:, --password:              user password to use when connecting to web service [$Password]"
	echo "-db:, --database:             database name to use [from app.json by default]"
	echo "-cs:, --connection-string:    connection string name to use from app.json [$Connection]"
	echo "-t:, --timeout:               timeout in seconds when checking connection to database and to web service, 0 - unlimited, -1 - default from app.json [$CheckTimeout]"
	echo "-tp:, --tools-path:           path to a folder with ${TESSA^^} command line tools \"tadmin\" (absolute or relative to current dir) [$Tools]"
	echo "-cfg:, --configuration-path:  path to installation folder with configuration to export (absolute or relative to tools path) [$Configuration]"
	echo "-?, --help                    this help page"
	echo
	echo "Example:"
	echo "${BASH_SOURCE[0]} -a:https://${TESSA,,}-server.com -u:admin -p:adminpswd -db:${TESSA,,} -cfg:$HOME/${TESSA,,}/Configuration"
}

#Arguments
Interactive="1"
QuietParam="-q"

for i in "$@"; do
	case $i in
		-d|--default)
			;;
		-v|--verbose)
			QuietParam="-nologo";;
		-a:*|--address:*)
			Address="${i#*:}";;
		-u:*|--user:*)
			Login="${i#*:}";;
		-p:*|--password:*)
			Password="${i#*:}";;
		-db:*|--database:*)
			Database="${i#*:}";;
		-cs:*|--connection-string:*)
			Connection="${i#*:}";;
		-t:*|--timeout:*)
			CheckTimeout="${i#*:}";;
		-tp:*|--tools-path:*)
			Tools="${i#*:}";;
		-cfg:*|--configuration-path:*)
			Configuration="${i#*:}";;
		-\?|--help)
			show_help && exit 0 ;;
		-*|--*)
			echo "Unknown option $i. Use --help for a list of valid options" && exit 2 ;;
		*)
			;;
	esac

	shift
	Interactive="0"
done

#Start
cd "$Tools" #if tools not found - get an error here
exit_on_error

if [ "$Interactive" == "1" ]; then
	echo "This script will export configuration for an existing ${TESSA^^} installation"
	echo "Use --help option for automation scenarios"
	echo
	echo "Please check connection string prior to installation in configuration file:"
	echo "$Tools/app-db.json"
	echo
fi

#default vars
if [ "$Database" == "" ]; then
	DbParam=""
	CheckDbParam="-db:"
else
	DbParam="-db:$Database"
	CheckDbParam="-db:$Database"
fi

#print vars
echo "[Address] = $Address"
if [ "$Database" == "" ]; then
	echo "[Database] = (from app.json)"
else
	echo "[Database] = $Database"
fi
echo "[Connection] = $Connection"
echo "[Tools] = $Tools"
echo "[Configuration] = $Configuration"

if [ "$Interactive" == "1" ]; then
	echo
	read -n1 -r -p "Press any key to begin the export or Ctrl+C to exit..." key
fi

#install
echo
echo "Exporting ${TESSA^^} configuration"
echo

echo " > Checking connection to database server"
./tadmin CheckDatabase -c "-cs:$Connection" $CheckDbParam "-timeout:$CheckTimeout" $QuietParam
exit_on_error

dbms="$(./tadmin CheckDatabase "-cs:$Connection" $CheckDbParam "-timeout:$CheckTimeout" -dbms)"
exit_on_error

echo "   DBMS = $dbms"
echo
if [ "$dbms" == "" ]; then
	exit 1
fi

echo " > Checking connection to web service"
./tadmin CheckService "-a:$Address" "-u:$Login" "-p:$Password" -timeout:$CheckTimeout $QuietParam
exit_on_error

echo

echo " > Exporting cards"
rm -rf "$Configuration/Cards/"
# types are exported in the same order they should be imported

echo "   - Settings"
# CardTypeFlags: Hidden=false, Singleton=true, Administrative=true

echo 'SELECT i."ID", t."Caption" FROM "Types" t INNER JOIN "Instances" i ON i."TypeID"=t."ID" WHERE (t."Flags" & 400) = 384 ORDER BY t."Caption"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Settings" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Currencies"
echo 'SELECT "ID", "Name" FROM "Currencies" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Currencies" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Calendar calculation methods"
echo 'SELECT "ID", "Name" FROM "CalendarCalcMethods" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Calendars" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Calendar types"
echo 'SELECT "ID", "Caption" FROM "CalendarTypes" ORDER BY "Caption"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Calendars" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Calendars"
echo 'SELECT "ID", "Name" FROM "CalendarSettings" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Calendars" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Format settings"
echo 'SELECT "ID", "Name" FROM "FormatSettings" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Format settings" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Roles: role generators"
echo 'SELECT "ID", "Name" FROM "RoleGenerators" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Roles" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Roles: static roles, dynamic roles, context roles"
echo 'SELECT "ID", "Name" FROM "Roles" WHERE "TypeID" IN (0, 3, 4) ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Roles" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Report permissions"
echo 'SELECT "ID", "Caption" FROM "ReportRolesRules" ORDER BY "Caption"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Report permissions" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Document types"
echo 'SELECT "ID", "Title" FROM "KrDocType" ORDER BY "Title"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Document types" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Reference group types"
echo 'SELECT "ID", "Name" FROM "RefGroupTypes" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:RefGroupTypes" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Reference groups"
echo 'SELECT "ID", "Name" FROM "RefGroups" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:RefGroups" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - KrProcess: stage templates"
echo 'SELECT "ID", "Name" FROM "KrStageTemplates" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:KrProcess" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - KrProcess: stage groups"
echo 'SELECT "ID", "Name" FROM "KrStageGroups" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:KrProcess" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - KrProcess: secondary processes"
echo 'SELECT "ID", "Name" FROM "KrSecondaryProcesses" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:KrProcess" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Task history group types"
echo 'SELECT "ID", "Caption" FROM "TaskHistoryGroupTypes" ORDER BY "Caption"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Task history group types" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - TaskKinds"
echo 'SELECT "ID", "Caption" FROM "TaskKinds" ORDER BY "Caption"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:TaskKinds" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Notification types"
echo 'SELECT "ID", "Name" FROM "NotificationTypes" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:NotificationTypes" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Notifications"
echo 'SELECT "ID", "Name" FROM "Notifications" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Notifications" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - ConditionTypes"
echo 'SELECT "ID", "Name" FROM "ConditionTypes" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:ConditionTypes" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Access rules"
echo 'SELECT "ID", "Caption" FROM "KrPermissions" ORDER BY "Caption"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/Platform.jcardlib" "-o:Access rules" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - File templates"
echo 'SELECT "ID", "Name" FROM "FileTemplates" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/PlatformWithFiles.jcardlib" "-o:File templates" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - VirtualFiles"
echo 'SELECT "ID", "Name" FROM "KrVirtualFiles" ORDER BY "Name"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/PlatformWithFiles.jcardlib" "-o:VirtualFiles" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo "   - Help sections"
echo 'SELECT "ID", CASE WHEN "LanguageCode" IS NULL THEN "Code" ELSE CONCAT("Code", '"'.'"', "LanguageCode") END AS "Digest" FROM "HelpSections" ORDER BY "Code", "LanguageCode"'|./tadmin Select $DbParam "-cs:$Connection" -q|./tadmin ExportCards "-l:$Configuration/Cards/PlatformWithFiles.jcardlib" "-o:HelpSections" "-a:$Address" "-u:$Login" "-p:$Password" -localize:en $QuietParam
exit_on_error

echo

echo " > Exporting localization"
./tadmin ExportLocalization "-o:$Configuration/Localization" "-a:$Address" "-u:$Login" "-p:$Password" -c $QuietParam
exit_on_error

echo " > Exporting scheme"
./tadmin ExportScheme "-o:$Configuration/Scheme" "-a:$Address" "-u:$Login" "-p:$Password" $QuietParam
exit_on_error

echo " > Exporting types"
./tadmin ExportTypes "-o:$Configuration/Types" "-a:$Address" "-u:$Login" "-p:$Password" -s -c $QuietParam
exit_on_error

echo " > Exporting forms"
./tadmin ExportForms "-o:$Configuration/Forms" "-a:$Address" "-u:$Login" "-p:$Password" $QuietParam
exit_on_error

echo " > Exporting settings"
./tadmin ExportSettings "-o:$Configuration/Settings" "-a:$Address" "-u:$Login" "-p:$Password" -c $QuietParam
exit_on_error

echo " > Exporting views"
./tadmin ExportViews "-o:$Configuration/Views" "-a:$Address" "-u:$Login" "-p:$Password" -s -c $QuietParam
exit_on_error

echo " > Exporting workplaces"
./tadmin ExportWorkplaces "-o:$Configuration/Workplaces" "-a:$Address" "-u:$Login" "-p:$Password" -c $QuietParam
exit_on_error

echo
echo "${TESSA^^} configuration has been exported to $Configuration"
exit 0
