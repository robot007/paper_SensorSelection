function F=CostFunSigMF(c,lqi)
% global lqi
% global prr
F=1./(1+exp(-c(2)*(lqi-c(1))));