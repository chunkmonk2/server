#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo ""

if [ $# -gt 1 -a "$1" == "push" ]
then
    TAG=$2

    echo "Pushing Bitwarden ($TAG)"
    echo "========================"
    
    docker push chunkmonk2/api:$TAG
    docker push chunkmonk2/identity:$TAG
    docker push chunkmonk2/server:$TAG
    docker push chunkmonk2/attachments:$TAG
    docker push chunkmonk2/icons:$TAG
    docker push chunkmonk2/notifications:$TAG
    docker push chunkmonk2/admin:$TAG
    docker push chunkmonk2/nginx:$TAG
    docker push chunkmonk2/mssql:$TAG
    docker push chunkmonk2/setup:$TAG
elif [ $# -gt 1 -a "$1" == "tag" ]
then
    TAG=$2
    
    echo "Tagging Bitwarden as '$TAG'"
    
    docker tag bitwarden/api chunkmonk2/api:$TAG
    docker tag bitwarden/identity chunkmonk2/identity:$TAG
    docker tag bitwarden/server chunkmonk2/server:$TAG
    docker tag bitwarden/attachments chunkmonk2/attachments:$TAG
    docker tag bitwarden/icons chunkmonk2/icons:$TAG
    docker tag bitwarden/notifications chunkmonk2/notifications:$TAG
    docker tag bitwarden/admin chunkmonk2/admin:$TAG
    docker tag bitwarden/nginx chunkmonk2/nginx:$TAG
    docker tag bitwarden/mssql chunkmonk2/mssql:$TAG
    docker tag bitwarden/setup chunkmonk2/setup:$TAG
else
    echo "Building Bitwarden"
    echo "=================="

    chmod u+x $DIR/src/Api/build.sh
    $DIR/src/Api/build.sh

    chmod u+x $DIR/src/Identity/build.sh
    $DIR/src/Identity/build.sh

    chmod u+x $DIR/util/Server/build.sh
    $DIR/util/Server/build.sh

    chmod u+x $DIR/util/Nginx/build.sh
    $DIR/util/Nginx/build.sh

    chmod u+x $DIR/util/Attachments/build.sh
    $DIR/util/Attachments/build.sh

    chmod u+x $DIR/src/Icons/build.sh
    $DIR/src/Icons/build.sh

    chmod u+x $DIR/src/Notifications/build.sh
    $DIR/src/Notifications/build.sh

    chmod u+x $DIR/src/Admin/build.sh
    $DIR/src/Admin/build.sh

    chmod u+x $DIR/util/MsSql/build.sh
    $DIR/util/MsSql/build.sh

    chmod u+x $DIR/util/Setup/build.sh
    $DIR/util/Setup/build.sh
fi
