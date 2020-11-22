# IngressRGPD
To extract Unique Portal Visit (upv) and Capture (upc), use the api with the following command line (Bash):

`curl --location --request POST 'https://ingressrgpd.azurewebsites.net/api/SendRawData' \
--form '=@/<path to your files>/game_log.tsv'`

Use the returned data with the plugin https://cdn.rawgit.com/Jormund/import-heatmap/master/import-heatmap.user.js

