start C:\Test\CRS\ClusterRenderingSample.exe -server 2 10.86.99.55:4767 *:* -test 0 -time 20 -port 11001
.\client.exe submit /session 1 /group 0 /command cmd.exe "/C" "start C:\Test\CRS\ClusterRenderingSample.exe -client 0 10.86.99.55:4767 *:* -enablelist 1 -test 0 -time 20 -port 11001"
.\client.exe submit /session 1 /group 0 /command cmd.exe "/C" "start C:\Test\CRS\ClusterRenderingSample.exe -client 1 10.86.99.55:4767 *:* -enablelist 0:2 -test 0 -time 20 -port 11001"
.\client.exe wait /session 1 /group 0
