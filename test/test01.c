#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	int32_t a = 4095;
	int8_t* b = (int8_t*)&a;

	printf("Val: %d\n", *(b + 0));

	return 0;
}