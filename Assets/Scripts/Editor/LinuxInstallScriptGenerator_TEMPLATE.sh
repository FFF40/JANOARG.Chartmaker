
set -e # For failsafe
if [ "$(uname -m)" != "x86_64" ]; then
    echo -e "\e[91mThis program only supports 64-bit systems (x86_64). Exiting.\e[0m"
    exit 1
fi

if [ ! -d './Chartmaker_Data' ] || [ ! -f 'Chartmaker.x86_64' ] || [ ! -f 'UnityPlayer.so' ]; then
 # Check for important files
    echo "One or more files are missing, make sure you're in the right directory. Exiting"
    exit 1
fi

AddSpecFiles(){
    case ${1} in
    Arch)
    cat > "./PKGBUILD" <<EOF_PKGBUILD
pkgname="${PACKAGE_NAME}-bin"    # Package name (-bin because it's a prebuilt binary)
pkgver="$PACKAGE_VERSION"        # Version
pkgrel=$PACKAGE_RELEASE          # Release number; IMPORTANT, INCREMENT THE VALUE ON EVERY RELEASE, otherwise package manager cannot update

url="https://github.com/FFF40/JANOARG-Chartmaker/"


pkgdesc="A standalone chartmaker for Just Another Normal, Ordinarily Acceptable Rhythm Game (JANOARG)" # Description
arch=("x86_64")                  # Supported architectures

# depends=()                     # Required dependencies: Your program *needs* it or it'll break
# optdepends=()                  # Optional dependencies: Your program doesn't need it, but will appreciate it if it's installed
# conflicts=()                   # Conflicts: Your program *will* break if the user have it

# Refer to https://github.com/FFF40/JANOARG-Chartmaker/blob/chartmaker/LICENSE.md
# No license file provided upstream as of this release.
# Declared as "custom" per AUR guidelines, but not installed.
license=("custom") # Doesn't seem to have real license

# Include files (run MAKEPKG.sh pls)
source=("UnityPlayer.so" "Chartmaker.x86_64" "Chartmaker_Data.tar" "icon.png")

# For file download verification, using "SKIP" isn't recommended but it's there for the sake of convenience when writing
sha512sums=("SKIP" "SKIP" "SKIP" "SKIP")

prepare() {
    tar -xf Chartmaker_Data.tar
}

build() {
    : # Doesn't need to do anything here, AUR just sometimes will break without this
}

package() {
    INSTALL_DIR="\${pkgdir}$INSTALL_PATH"

    # Create the install dir
    mkdir -p "\$INSTALL_DIR"

    echo "Copying binaries..."
    install -Dm755 "\${srcdir}/UnityPlayer.so" "\$INSTALL_DIR/UnityPlayer.so"
    install -Dm755 "\${srcdir}/Chartmaker.x86_64" "\$INSTALL_DIR/Chartmaker.x86_64" # For ease of typing

    # Ensure Chartmaker_Data was extracted
    if [ ! -d "\${srcdir}/Chartmaker_Data" ]; then
        prepare
    fi

    # Copy data files
    mkdir -p "\$INSTALL_DIR/Chartmaker_Data"
    cp -vr "\${srcdir}/Chartmaker_Data/"* "\$INSTALL_DIR/Chartmaker_Data/"

    # Fix permission the primitive way just in case
    chmod +x "\${srcdir}/Chartmaker.x86_64"

    # Create symlink to allow launching from terminal
    mkdir -p "\${pkgdir}/usr/bin"
    ln -s "/opt/JANOARG-Chartmaker/Chartmaker.x86_64" "\${pkgdir}/usr/bin/JANOARG-Chartmaker"

    # Copy to a FreeDesktop compliant path
    install -Dm644 "\${srcdir}/icon.png" "\${pkgdir}/usr/share/icons/hicolor/128x128/apps/JANOARG-Chartmaker.png"


    # Create .desktop file for GUI launchers
    install -Dm644 /dev/stdin "\${pkgdir}/usr/share/applications/JANOARG-Chartmaker.desktop" <<EOF_DESKTOP
[Desktop Entry]
Name=JANOARG Chartmaker
Exec=JANOARG-Chartmaker
Icon=JANOARG-Chartmaker
Comment=A standalone chartmaker for Just Another Normal, Ordinarily Acceptable Rhythm Game (JANOARG)
Type=Application
Terminal=false
Categories=Game;
EOF_DESKTOP
}
EOF_PKGBUILD

    ;;
    Debian)
    CONTROL_FILE_PATH="./\${FAKEROOT_NAME}/DEBIAN/control"
    cat > "./${CONTROL_FILE_PATH}/DEBIAN/control" <<EOF_DEBIAN
Package: $PACKAGE_NAME
Version: $PACKAGE_VERSION
Section: games
Priority: optional
Architecture: amd64
Maintainer: BashhScriptKid <contact@bashh.slmail.me>
Description: A standalone chartmaker for Just Another Normal, Ordinarily Acceptable Rhythm Game (JANOARG)
EOF_DEBIAN

    ;;

    *)
    :
    ;;

    esac
}

AddDesktopShortcut(){
    install -Dm644 /dev/stdin "${1}/usr/share/applications/JANOARG-Chartmaker.desktop" <<EOF_DESKTOP
[Desktop Entry]
Name=JANOARG Chartmaker
Exec=JANOARG-Chartmaker
Icon=JANOARG-Chartmaker
Comment=A standalone chartmaker for Just Another Normal, Ordinarily Acceptable Rhythm Game (JANOARG)
Type=Application
Terminal=false
Categories=Game;
EOF_DESKTOP
}

log(){
    bold='\e[1m'
    lightgreen='\e[92m'
    blue='\e[34m'
    C_reset='\e[0m'
    Bold_reset='\e[21m'
    echo -e "${bold}${lightgreen}==> ${blue}${1}${C_reset}"
}

# Copy app icon
cp "./Chartmaker_Data/Resources/UnityPlayer.png" "./icon.png"

if [ -x "$(command -v pacman)" ]; then # Arch-based
    log "Using PKGBUILD."

    trap 'rm -f Chartmaker_Data.tar PKGBUILD' EXIT # Clean up on exit

    AddSpecFiles Arch

    log "Creating Chartmaker_Data tarball."
    tar -cf Chartmaker_Data.tar ./Chartmaker_Data/ # Make tarball on runtime as PKGBUILD can't add directory as source

    makepkg -ci OPTIONS=-debug
elif [ -x "$(command -v apt)" ] || [ -x "$(command -v apt-get)" ]; then # Debian-based
    ## These code are untested

    log "Using dpkg-deb."

    FAKEROOT_NAME="${PACKAGE_NAME}_${PACKAGE_VERSION}-${PACKAGE_RELEASE}"

    # Prepare fakeroot directory
    mkdir -p "${FAKEROOT_NAME}${INSTALL_PATH}/Chartmaker_Data"
    mkdir -p "${FAKEROOT_NAME}/usr/share/applications"
    mkdir -p "${FAKEROOT_NAME}/usr/bin"

    # Copy files
    cp -v "./UnityPlayer.so" "${FAKEROOT_NAME}${INSTALL_PATH}/"
    cp -v "./Chartmaker.x86_64" "${FAKEROOT_NAME}${INSTALL_PATH}/"
    cp -v "./icon.png" "${FAKEROOT_NAME}${INSTALL_PATH}/"
    cp -vr "./Chartmaker_Data/"* "${FAKEROOT_NAME}${INSTALL_PATH}/Chartmaker_Data/"

    # Link main binary
    ln -s "../../opt/$PACKAGE_NAME/Chartmaker.x86_64" "${FAKEROOT_NAME}/usr/bin/JANOARG-Chartmaker"
    AddDesktopShortcut "${FAKEROOT_NAME}"

    # Add control files
    mkdir "${FAKEROOT_NAME}/DEBIAN/"

    AddSpecFiles Debian

    # Fix permissions
    chmod -v +x ${FAKEROOT_NAME}/*

    # Build the deb file
    dpkg-deb --build "${FAKEROOT_NAME}" "${FAKEROOT_NAME}.deb"

    # Copy to a FreeDesktop compliant path
    mkdir -p "${FAKEROOT_NAME}/usr/share/icons/hicolor/128x128/apps"
    cp "./icon.png" "${FAKEROOT_NAME}/usr/share/icons/hicolor/128x128/apps/JANOARG-Chartmaker.png"


    # Install it (finally)
    dpkg -i "${FAKEROOT_NAME}.deb"

    log "Cleaning up..."
    rm -r ${FAKEROOT_NAME}

elif [ -x "$(command -v dnf)" ]; then
    echo -e "\e[33m\e[1mI haven't implemented installation procedures for Fedora-based systems yet, sorry! \nPlease refer to https://asamalik.fedorapeople.org/tmp-docs-preview/quick-docs/creating-rpm-packages/ if you think you can help! \e[0m"

else
    echo -e "\e[1m\e[91mYeah I dunno what distro that is, probably not gonna bother.\e[0m "
    exit 1
fi

log "To uninstall, simply delete '${INSTALL_PATH}'."
exit
