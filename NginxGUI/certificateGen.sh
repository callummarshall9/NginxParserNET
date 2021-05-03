subject="%subject%"
csrPath="%csrPath%"
keyPath="%keyPath%"
crtPath="%crtPath%"
pemPath="%pemPath%"
caTrustPath="/etc/pki/ca-trust/source/anchors/"

openssl req --nodes --newkey rsa:2048 --keyout "$keyPath" -out "$csrPath" --subj "$subject"
openssl x509 -in "$csrPath" -out "$crtPath" -req -signkey "$keyPath" -days 3650
openssl x509 -in "$crtPath" -out "$pemPath" -outform PEM
mv "$pemPath" "$caTrustPath"
update-ca-trust
