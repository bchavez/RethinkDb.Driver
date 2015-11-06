

function RunTests

        http "https://ci.appveyor.com/api/buildjobs/$AppVeyorJobId/artifacts/RethinkDb.Driver.Tests.zip" Authorization:"Bearer $AppVeyorToken" > DriverTests.zip
        unzip DriverTests.zip -d DriverTests
        cd DriverTests
        mono Runner/nunit-console.exe UnitTests/RethinkDb.Driver.Tests.dll

end

function Setup

      set -x DISTRIB_CODENAME precise

      #Sane HTTP
      sudo pip install -U httpie

      #Install RethinkDB
      echo "deb http://download.rethinkdb.com/apt $DISTRIB_CODENAME main" | sudo tee /etc/apt/sources.list.d/rethinkdb.list
      wget -qO- http://download.rethinkdb.com/apt/pubkey.gpg | sudo apt-key add -
      sudo apt-get update -qq
      sudo apt-get -y install rethinkdb
      sudo cp /etc/rethinkdb/default.conf.sample /etc/rethinkdb/instances.d/instance1.conf
      sudo /etc/init.d/rethinkdb restart

      # Fix Circle CI's jacked up mono install. :\ ... +5 mins.
      sudo apt-get install aptitude
      sudo apt-get --reinstall install libept1.4.12

      sudo aptitude purge mono-devel -y
      sudo aptitude purge mono-runtime -y

      # add legit mono.
      sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
      echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
      echo "deb http://download.mono-project.com/repo/debian wheezy-libtiff-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list
      sudo apt-get update -qq

      sudo aptitude install mono-devel -y

end

if test $AppVeyorJobId
        if test $argv[1] = "Setup"
                Setup
        end
        if test $argv[1] = "RunTests"
                RunTests
        else
            echo "Invalid command"
        end
else
     echo "No webhost defined"
end
