#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	uint32_t a = 4;
	
	while(a > 0) {
		printf("A: %d\n", a);
		a--;
	}
	
	return 0;
}