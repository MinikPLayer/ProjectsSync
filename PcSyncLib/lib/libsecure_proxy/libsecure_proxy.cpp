#include <iostream>

extern "C" {
    int hello_world();
}

int hello_world() {
    std::cout << "Hello, world!" << std::endl;
    return 12345;
}