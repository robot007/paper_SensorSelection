function RangeLightMatv6(dist,rangelight)
%RANGELIGHTMATV6    Create plot of datasets and fits
%   RANGELIGHTMATV6(DIST,RANGELIGHT)
%   Creates a plot, similar to the plot in the main curve fitting
%   window, using the data that you provide as input.  You can
%   apply this function to the same data you used with cftool
%   or with different data.  You may want to edit the function to
%   customize the code and this help message.
%
%   Number of datasets:  1
%   Number of fits:  1

 
% Data from dataset "rangelight vs. dist":
%    X = dist:
%    Y = rangelight:
%    Unweighted
%
% This function was automatically generated

% Set up figure to receive datasets and fits
f_ = clf;
figure(f_);
legh_ = []; legt_ = {};   % handles and text for legend
xlim_ = [Inf -Inf];       % limits of x axis
ax_ = subplot(1,1,1);
set(ax_,'Box','on');
grid(ax_,'on');
axes(ax_); hold on;

 
% --- Plot data originally in dataset "rangelight vs. dist"
dist = dist(:);
rangelight = rangelight(:);
h_ = line(dist,rangelight,'Parent',ax_,'Color',[0.333333 0 0.666667],...
     'LineStyle','none', 'LineWidth',2,...
     'Marker','x', 'MarkerSize',12);
xlim_(1) = min(xlim_(1),min(dist));
xlim_(2) = max(xlim_(2),max(dist));
legh_(end+1) = h_;
legt_{end+1} = 'Raw data';

% Nudge axis limits beyond data limits
if all(isfinite(xlim_))
   xlim_ = xlim_ + [-1 1] * 0.01 * diff(xlim_);
   set(ax_,'XLim',xlim_)
end


% --- Create fit "Quad"
ft_ = fittype('poly2' );

% Fit this model using new data
cf_ = fit(dist,rangelight,ft_ );

% Or use coefficients from the original fit:
% if 1
%    cv_ = {2.523653896375, -215.9941913074, 6719.678601399};
%    cf_ = cfit(ft_,cv_{:});
% end
% 
% disp(['Light-dist. model fitted parameters ' num2str(cv_{1}) ',' num2str(cv_{2}) ',' num2str(cv_{3})]);

% Plot this fit
h_ = plot(cf_,'predobs',0.95);
legend off;  % turn off legend from plot method call
set(h_(1),'Color',[1 0 0],...
     'LineStyle','-', 'LineWidth',2,...
     'Marker','none', 'MarkerSize',12);
legh_(end+1) = h_(1);
legt_{end+1} = 'Quadric fit';
if length(h_)>1
   set(h_(2:end),'Color',[1 0 0],...
       'LineStyle',':', 'LineWidth',2,'Marker','none');
   legh_(end+1) = h_(2);
   legt_{end+1} = '95% confidence bound';
end

hold off;
legend(ax_,legh_, legt_);
xlabel('Distance (cm)');
ylabel('Light reading');
