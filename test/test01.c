#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>

int main(int argc, char **argv)
{
	int a = 5;

	if(a < 5) 
	{
		printf("1\n");
	} 
	else if(a < 8)
	{
		printf("2\n");
	} 
	else if(a < 10)
	{
		printf("3\n");
	}
	else 
	{
		printf("4\n");
	}

	return 0;
}