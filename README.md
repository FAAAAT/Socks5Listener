# Socks5Listener

## What is it?

  A data transmission from tcp to socks5. Listen a local port and tranfer data to a specified local/remote socks5 server.

## How to use?

  Suppose you have a vps and a local machine. You want to do something awesome but your local machine do not have a public IP address and your vps sucks. Then you can deploy this project and [ssocks](https://sourceforge.net/projects/ssocks/)/[ssocks for windows](https://github.com/tostercx/ssocks) (in the project you can find windows version also) in your vps and local machine. Link them and enjoy!

  1. Deploy rcsocks in your vps. Argument -p specified the port that Socks5Listener will connect to. -l specified the port that rssocks will relay to;

  2. Deploy Socks5Listener to your vps. Argument -l specified the address:port tcp will connect. -p specified the address:port rcsocks is listening. -d specified the address:port your local machine opend.

  3. Deploy rssocks in your localsystem. Argument --socks specified the address:port that data will relay to. --ncon specified MAX number of connections will accepted.

  4. Use vps:port as your local system port. you will see the content in your local system port.

## TODO

  1. Listener DO NOT support IPv6 and UDP. MAY NOT BE support socks5 type bind(not tested,I not find the code).

  2. Unit test is ON THE WAY.

  3. Improve my english.

  4. I think it can use as an 6to4/6in4 server in the future.
