#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>

void test(char* format, ...) {

}

int main(int argc, char **argv)
{
	int16_t a = 5;
	test("", a);
	return 0;
}